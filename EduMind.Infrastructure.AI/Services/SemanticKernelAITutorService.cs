using EduMind.Application.Interfaces;
using EduMind.Application.Models;
using EduMind.Infrastructure.AI.Caching;
using EduMind.Infrastructure.AI.Configuration;
using EduMind.Infrastructure.AI.TextChunking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Text;

namespace EduMind.Infrastructure.AI.Services;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════════════════╗
/// ║         SemanticKernelAITutorService — "Bộ não" của EduMind AI Tutor       ║
/// ╠══════════════════════════════════════════════════════════════════════════════╣
/// ║  Implement IAITutorService bằng Microsoft Semantic Kernel + Google Gemini.  ║
/// ║                                                                              ║
/// ║  Luồng RAG (Retrieval-Augmented Generation) đầy đủ:                         ║
/// ║                                                                              ║
/// ║  [INGEST]                                                                    ║
/// ║  PDF Stream → PdfPig (parse) → TextChunker (chunk) →                        ║
/// ║  Gemini Embedding API (vectorize) → Qdrant (store)                           ║
/// ║                                                                              ║
/// ║  [QUERY]                                                                     ║
/// ║  Question → Redis Cache? → Gemini Embedding API (vectorize question) →       ║
/// ║  Qdrant (retrieve top-K chunks) → Build Prompt → Gemini Chat API (generate)  ║
/// ║  → Cache result → Return AiTutorResponse                                     ║
/// ╚══════════════════════════════════════════════════════════════════════════════╝
/// </summary>
public sealed class SemanticKernelAITutorService : IAITutorService
{
    // ── Dependencies (được inject qua DI) ──────────────────────────────────────
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
#pragma warning disable CS0618 // Type or member is obsolete in SK 1.79
    private readonly ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore CS0618
    private readonly IDocumentParsingService _documentParsingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly RedisCacheService _cacheService;
    private readonly TextChunker _textChunker;
    private readonly AISettings _settings;
    private readonly ILogger<SemanticKernelAITutorService> _logger;

    // ── System Prompt cho Gemini — định hướng hành vi của AI Tutor ─────────────
    private const string SystemPrompt = """
        Bạn là EduMind AI Tutor — một gia sư AI thông minh, nhiệt tình và am hiểu sâu về tài liệu học tập.
        
        NHIỆM VỤ CỦA BẠN:
        - Trả lời câu hỏi của học sinh DỰA TRÊN ngữ cảnh tài liệu được cung cấp bên dưới.
        - Giải thích rõ ràng, dễ hiểu, có ví dụ minh họa khi cần.
        - Nếu câu hỏi KHÔNG liên quan đến tài liệu, hãy thông báo lịch sự và gợi ý học sinh xem lại tài liệu.
        - Ngôn ngữ trả lời: tiếng Việt (trừ khi học sinh hỏi bằng tiếng Anh).
        - KHÔNG bịa đặt thông tin ngoài ngữ cảnh tài liệu.
        """;

    public SemanticKernelAITutorService(
        Kernel kernel,
        IDocumentParsingService documentParsingService,
        IVectorStoreService vectorStoreService,
        RedisCacheService cacheService,
        TextChunker textChunker,
        IOptions<AISettings> settings,
        ILogger<SemanticKernelAITutorService> logger)
    {
        _kernel = kernel;
        _documentParsingService = documentParsingService;
        _vectorStoreService = vectorStoreService;
        _cacheService = cacheService;
        _textChunker = textChunker;
        _settings = settings.Value;
        _logger = logger;

        // Lấy các service từ Kernel (đã được đăng ký khi cấu hình DI)
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
#pragma warning disable CS0618
        _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LUỒNG 1: QUERY — Trả lời câu hỏi dựa trên tài liệu (RAG Pipeline)
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<AiTutorResponse> AskQuestionAsync(
        AiTutorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AskQuestion: CollectionName='{Collection}', Question='{Question}'",
            request.CollectionName, request.Question[..Math.Min(50, request.Question.Length)]);

        // ── Bước 1: Kiểm tra cache Redis ──────────────────────────────────────
        // Nếu câu hỏi này đã được trả lời trước đó → trả về kết quả cached ngay lập tức
        var cacheKey = RedisCacheService.BuildCacheKey(request.CollectionName, request.Question);
        var cachedResponse = await _cacheService.GetAsync<AiTutorResponse>(cacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            _logger.LogInformation("Trả về kết quả từ Redis cache cho câu hỏi: '{Question}'", request.Question[..Math.Min(50, request.Question.Length)]);
            return cachedResponse;
        }

        // ── Bước 2: Embed câu hỏi thành vector ───────────────────────────────
        // Chuyển câu hỏi thành vector embedding để so sánh với các chunk trong Qdrant
        var questionEmbedding = await GenerateEmbeddingAsync(request.Question, cancellationToken);

        // ── Bước 3: Retrieval — Tìm các chunk liên quan nhất trong Qdrant ─────
        var relevantChunks = await _vectorStoreService.SearchSimilarChunksAsync(
            request.CollectionName,
            questionEmbedding,
            request.TopKChunks,
            cancellationToken);

        if (relevantChunks.Count == 0)
        {
            _logger.LogWarning(
                "Không tìm thấy chunk nào liên quan trong collection '{Collection}'",
                request.CollectionName);

            return new AiTutorResponse
            {
                Answer = "Xin lỗi, tôi không tìm thấy thông tin liên quan trong tài liệu học tập. " +
                         "Hãy thử đặt câu hỏi theo cách khác hoặc kiểm tra lại tài liệu đã được tải lên.",
                RelevantChunks = [],
                TokensUsed = 0
            };
        }

        // ── Bước 4: Generation — Xây dựng prompt và gọi Gemini AI ────────────
        var answer = await GenerateAnswerWithGeminiAsync(request.Question, relevantChunks, cancellationToken);

        // ── Bước 5: Lưu kết quả vào Redis cache ──────────────────────────────
        var response = new AiTutorResponse
        {
            Answer = answer,
            RelevantChunks = relevantChunks.ToList(),
            GeneratedAt = DateTimeOffset.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, response, cancellationToken);

        return response;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LUỒNG 2: INGEST — Nhập tài liệu PDF vào hệ thống RAG
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task IngestDocumentAsync(
        Stream pdfStream,
        string fileName,
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu Ingest tài liệu: '{FileName}' vào collection '{Collection}'", fileName, collectionName);

        // ── Bước 1: Đảm bảo Qdrant collection tồn tại ────────────────────────
        await _vectorStoreService.EnsureCollectionExistsAsync(
            collectionName,
            _settings.EmbeddingDimension,
            cancellationToken);

        // ── Bước 2: Bóc tách text từ PDF bằng PdfPig ─────────────────────────
        var parsedDocument = await _documentParsingService.ParsePdfAsync(pdfStream, fileName, cancellationToken);

        _logger.LogInformation(
            "Đã bóc tách xong PDF '{FileName}': {Pages} trang, {Chars} ký tự",
            fileName, parsedDocument.TotalPages, parsedDocument.RawText.Length);

        // ── Bước 3: Chunking — Chia văn bản thành các đoạn nhỏ ───────────────
        // Sử dụng TextChunker với cấu hình từ AISettings (chunkSize, overlap)
        var textChunks = _textChunker.Chunk(
            parsedDocument.RawText,
            _settings.ChunkSize,
            _settings.ChunkOverlap);

        _logger.LogInformation(
            "Đã chia '{FileName}' thành {ChunkCount} chunks (size={Size}, overlap={Overlap})",
            fileName, textChunks.Count, _settings.ChunkSize, _settings.ChunkOverlap);

        // ── Bước 4: Tạo Embedding Vector cho từng chunk ───────────────────────
        // Xử lý theo batch để tránh quá tải Gemini Embedding API
        const int embeddingBatchSize = 10; // Xử lý 10 chunk mỗi lần gọi API
        var documentChunks = new List<DocumentChunk>();

        for (var batchStart = 0; batchStart < textChunks.Count; batchStart += embeddingBatchSize)
        {
            // Lấy một batch chunk
            var batch = textChunks.Skip(batchStart).Take(embeddingBatchSize).ToList();

            _logger.LogDebug(
                "Đang embed batch {BatchIndex}/{TotalBatches}",
                batchStart / embeddingBatchSize + 1,
                (int)Math.Ceiling((double)textChunks.Count / embeddingBatchSize));

            // Gọi Gemini Embedding API cho toàn bộ batch (gọi một lần cho nhiều text = hiệu quả hơn)
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(batch, _kernel, cancellationToken);

            // Ghép chunk text + embedding vector thành DocumentChunk
            for (var i = 0; i < batch.Count; i++)
            {
                var globalChunkIndex = batchStart + i;
                documentChunks.Add(new DocumentChunk
                {
                    // ID dạng: "filename_chunk_000042" — dễ trace và deterministic
                    Id = $"{fileName}_chunk_{globalChunkIndex:D6}",
                    Content = batch[i],
                    SourceDocument = fileName,
                    ChunkIndex = globalChunkIndex,
                    // PageNumber: ước tính trang dựa trên vị trí chunk trong văn bản
                    // (chỉ là ước tính vì chunking không track số trang chính xác)
                    PageNumber = EstimatePageNumber(globalChunkIndex, textChunks.Count, parsedDocument.TotalPages),
                    // Chuyển ReadOnlyMemory<float> → float[] để lưu vào record
                    Embedding = embeddings[i].ToArray()
                });
            }

            // Delay nhỏ giữa các batch để tránh rate limiting của Gemini API
            if (batchStart + embeddingBatchSize < textChunks.Count)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        // ── Bước 5: Lưu tất cả chunks vào Qdrant ─────────────────────────────
        await _vectorStoreService.UpsertChunksAsync(collectionName, documentChunks, cancellationToken);

        _logger.LogInformation(
            "✅ Ingest hoàn tất: '{FileName}' → {ChunkCount} chunks đã lưu vào Qdrant collection '{Collection}'",
            fileName, documentChunks.Count, collectionName);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LUỒNG 3: DELETE — Xóa tài liệu khỏi hệ thống
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task DeleteDocumentAsync(
        string fileName,
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Xóa tài liệu '{FileName}' khỏi collection '{Collection}'", fileName, collectionName);

        await _vectorStoreService.DeleteBySourceDocumentAsync(collectionName, fileName, cancellationToken);

        _logger.LogInformation("✅ Đã xóa xong tài liệu '{FileName}'", fileName);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gọi Gemini Embedding API để chuyển một đoạn text thành float[] vector.
    /// </summary>
    private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync([text], _kernel, cancellationToken);
        return embeddings[0].ToArray();
    }

    /// <summary>
    /// Xây dựng prompt RAG và gọi Gemini Chat API để sinh câu trả lời.
    ///
    /// Cấu trúc prompt RAG chuẩn:
    ///   [System] Định nghĩa vai trò AI Tutor
    ///   [User]   CONTEXT: {các chunk liên quan}
    ///            QUESTION: {câu hỏi của học sinh}
    /// </summary>
    private async Task<string> GenerateAnswerWithGeminiAsync(
        string question,
        IReadOnlyList<DocumentChunk> relevantChunks,
        CancellationToken cancellationToken)
    {
        // ── Xây dựng phần CONTEXT từ các chunk đã retrieval ──────────────────
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("=== NGỮ CẢNH TÀI LIỆU HỌC TẬP ===");
        contextBuilder.AppendLine();

        for (var i = 0; i < relevantChunks.Count; i++)
        {
            var chunk = relevantChunks[i];
            contextBuilder.AppendLine($"[Đoạn {i + 1}] (Nguồn: {chunk.SourceDocument}, Trang: {chunk.PageNumber?.ToString() ?? "N/A"})");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine("---");
        }

        // ── Xây dựng ChatHistory để gửi cho Gemini ───────────────────────────
        var chatHistory = new ChatHistory(SystemPrompt);

        // Thêm user message gồm Context + Question theo định dạng RAG chuẩn
        chatHistory.AddUserMessage(
            $"""
            {contextBuilder}
            
            === CÂU HỎI CỦA HỌC SINH ===
            {question}
            
            Hãy trả lời câu hỏi dựa trên ngữ cảnh tài liệu ở trên.
            """);

        _logger.LogDebug("Gửi prompt đến Gemini (context = {ChunkCount} chunks)", relevantChunks.Count);

        // ── Gọi Gemini Chat Completion API qua Semantic Kernel ────────────────
        var chatResult = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        return chatResult.Content ?? "Xin lỗi, tôi không thể tạo câu trả lời lúc này. Vui lòng thử lại.";
    }

    /// <summary>
    /// Ước tính số trang tương ứng với một chunk dựa trên vị trí tương đối của nó.
    /// </summary>
    /// <param name="chunkIndex">Vị trí của chunk (0-indexed)</param>
    /// <param name="totalChunks">Tổng số chunk của tài liệu</param>
    /// <param name="totalPages">Tổng số trang của tài liệu</param>
    private static int EstimatePageNumber(int chunkIndex, int totalChunks, int totalPages)
    {
        if (totalChunks == 0 || totalPages == 0) return 1;

        // Tỉ lệ vị trí của chunk trong văn bản × tổng số trang
        var estimatedPage = (int)Math.Ceiling(((double)chunkIndex / totalChunks) * totalPages);
        return Math.Max(1, Math.Min(estimatedPage, totalPages));
    }
}

namespace Aelena.FileApi.Core.Enums;

/// <summary>Comparison algorithm to use when diffing two documents.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompareMode
{
    /// <summary>Character-level diff.</summary>
    String,
    /// <summary>Word-level unified diff.</summary>
    Lexical,
    /// <summary>LLM-powered semantic analysis.</summary>
    Semantic,
    /// <summary>Summarise both documents, then compare summaries.</summary>
    Summary
}

/// <summary>Format of the comparison or summarization output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutputFormat
{
    /// <summary>Machine-readable structured JSON.</summary>
    Structured,
    /// <summary>Human-readable narrative prose.</summary>
    Narrative
}

/// <summary>Tone / verbosity level for LLM-generated output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Tone
{
    Concise,
    Professional,
    Detailed,
    Legal
}

/// <summary>Section alignment strategy for document comparison.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlignmentMode
{
    /// <summary>Align by positional index (page number / paragraph number).</summary>
    Page,
    /// <summary>Align by semantic similarity.</summary>
    Semantic
}

/// <summary>
/// Controls where LLM calls are routed.
/// <list type="bullet">
///   <item><description><c>Private</c> — local model (OpenWebUI / Ollama).</description></item>
///   <item><description><c>Public</c> — cloud API (e.g. OpenAI GPT-4o).</description></item>
///   <item><description><c>AirGapped</c> — no LLM calls; offline-only algorithms.</description></item>
/// </list>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Confidentiality
{
    Public,
    Private,
    AirGapped
}

/// <summary>Document type classification labels.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentType
{
    Contract, Invoice, Receipt, Report, Letter, Memo, Email,
    LegalFiling, Regulation, Policy, TechnicalManual, Specification,
    AcademicPaper, Thesis, Resume, CoverLetter, Proposal,
    Presentation, Spreadsheet, Form, Certificate, Statement,
    Minutes, Agenda, Newsletter, PressRelease, Patent, Permit,
    WhitePaper, CaseStudy, FinancialReport, TaxDocument,
    MedicalRecord, InsuranceDocument, Other
}

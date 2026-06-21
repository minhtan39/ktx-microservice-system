using PdfSharpCore.Fonts;

namespace ContractStudentService.Services;

public sealed class ContractFontResolver : IFontResolver
{
    public const string FamilyName = "Times New Roman";

    private const string RegularFace = "Tinos-Regular";
    private const string BoldFace = "Tinos-Bold";
    private const string ItalicFace = "Tinos-Italic";
    private const string BoldItalicFace = "Tinos-BoldItalic";

    private readonly Dictionary<string, byte[]> _fonts;

    public ContractFontResolver(IWebHostEnvironment environment)
        : this(Path.Combine(
            environment.ContentRootPath,
            "Assets",
            "Fonts"))
    {
    }

    public ContractFontResolver(string fontDirectory)
    {
        _fonts = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RegularFace] = ReadFont(fontDirectory, "Tinos-Regular.ttf"),
            [BoldFace] = ReadFont(fontDirectory, "Tinos-Bold.ttf"),
            [ItalicFace] = ReadFont(fontDirectory, "Tinos-Italic.ttf"),
            [BoldItalicFace] = ReadFont(fontDirectory, "Tinos-BoldItalic.ttf")
        };
    }

    public string DefaultFontName => FamilyName;

    public byte[] GetFont(string faceName)
    {
        return _fonts.TryGetValue(faceName, out var font)
            ? font
            : _fonts[RegularFace];
    }

    public FontResolverInfo ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic)
    {
        var faceName = (isBold, isItalic) switch
        {
            (true, true) => BoldItalicFace,
            (true, false) => BoldFace,
            (false, true) => ItalicFace,
            _ => RegularFace
        };

        return new FontResolverInfo(faceName);
    }

    private static byte[] ReadFont(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy font PDF hợp đồng: {fileName}",
                path);
        }

        return File.ReadAllBytes(path);
    }
}

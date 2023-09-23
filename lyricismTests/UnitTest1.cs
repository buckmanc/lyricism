namespace lyricismTests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void AZLyricsExtractorTests()
    {
        // AZLyrics does not list Nytt Land as one of the artists
        LyricExtractorTest(new lyricism.Extractors.AZLyricsExtractor("Korpiklaani", "Shai Shai - Siberia"));
        LyricExtractorTest(new lyricism.Extractors.AZLyricsExtractor("Wrabel", "The Village"));
    }

    [TestMethod]
    public void BandcampExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.BandcampExtractor("feminazgul", "mother"));
        LyricExtractorTest(new lyricism.Extractors.BandcampExtractor("froglord", "amphibian ascending"));
    }

    [TestMethod]
    public void DarkLyricsExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.DarkLyricsExtractor("gloryhammer", "hootsforce"));
        LyricExtractorTest(new lyricism.Extractors.DarkLyricsExtractor("Korpiklaani", "Ievan Polkka"));
    }

    [TestMethod]
    public void GeniusExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.GeniusExtractor("feminazgul", "mother"));
        LyricExtractorTest(new lyricism.Extractors.GeniusExtractor("froglord", "amphibian ascending"));
    }

    [TestMethod]
    public void LetrasDotComExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.LetrasDotComExtractor("grailknights", "march of the skeletons"));
        LyricExtractorTest(new lyricism.Extractors.LetrasDotComExtractor("gloryhammer", "hootsforce"));
    }

    [TestMethod]
    public void LyricsDotComExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.LyricsDotComExtractor("chris housman", "bible belt"));
        LyricExtractorTest(new lyricism.Extractors.LyricsDotComExtractor("banshee", "witch"));
    }

    [TestMethod]
    public void MetallumExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.MetallumExtractor("feminazgul", "mother"));
        LyricExtractorTest(new lyricism.Extractors.MetallumExtractor("froglord", "amphibian ascending"));
    }

    [TestMethod]
    public void MojimExtractorTests()
    {
        LyricExtractorTest(new lyricism.Extractors.MojimExtractor("na fianna", "there's no one as irish as barack obama"));
        LyricExtractorTest(new lyricism.Extractors.MojimExtractor("babymetal", "ギミチョコ"));
    }

    [TestMethod]
    public void FileCacheExtractorTests()
    {
        // pull lyrics, write them to the cache, then pull them back out
        var lex = new lyricism.Extractors.GeniusExtractor("froglord", "amphibian ascending");
        LyricExtractorTest(lex);
        lex.Cache();
        LyricExtractorTest(new lyricism.Extractors.FileCacheExtractor("froglord", "amphibian ascending"));
    }

    internal void LyricExtractorTest(lyricism.LyricExtractor lex)
    {
        Assert.IsTrue((lex.Lyrics ?? string.Empty).Length > 20);
        Assert.IsTrue((lex.ArtistName ?? string.Empty).Length >= lex.SearchArtistName.Length);
        Assert.IsTrue((lex.TrackName ?? string.Empty).Length >= lex.SearchTrackName.Length);
    }

    [TestMethod]
    public void GetLyricReportTest()
    {
        var artistName = "Froglord";
        var trackName = "Amphibian Ascending";
        var dumpChars = new string[] {" ", "\r", "\n", "\t"};

        var reportLyrics = lyricism.Program.GetLyricReport(artistName, trackName, null, false, false, false, null).Join()
            .Replace(dumpChars, string.Empty);
        var exLyrics = (new lyricism.Extractors.GeniusExtractor(artistName, trackName)).Lyrics
            .Replace(dumpChars, string.Empty);

        Assert.IsTrue(reportLyrics.Contains(exLyrics));
    }
}

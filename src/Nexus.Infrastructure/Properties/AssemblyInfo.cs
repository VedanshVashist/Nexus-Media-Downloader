using System.Runtime.CompilerServices;

// Expose internal yt-dlp mapping types to the test project for JSON parsing tests.
[assembly: InternalsVisibleTo("Nexus.Tests")]

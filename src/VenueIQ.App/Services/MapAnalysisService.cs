using VenueIQ.Core.Models;
using VenueIQ.Core.Services;

namespace VenueIQ.App.Services;

public class MapAnalysisService
{
    private readonly ScoreCalculator _scoreCalculator;
    public MapAnalysisService(ScoreCalculator scoreCalculator)
    {
        _scoreCalculator = scoreCalculator;
    }

    public Task<AnalysisResultDto> AnalyzeAsync(CancellationToken ct = default)
        => Task.FromResult(new AnalysisResultDto());
}

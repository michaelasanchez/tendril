using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ScraperRepository(TendrilDbContext db) : IScraperRepository
{
    public async Task<List<ScraperDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ScraperDefinition>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Parameters)
            .Include(s => s.Selectors.Where(z => !z.Disabled))
            .Include(s => s.ClassificationRules.Where(z => !z.Disabled))
            .Include(s => s.MappingRules.Where(z => !z.Disabled))
            .ToListAsync(cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Parameters)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdWithDisabledDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Parameters)
            .Include(s => s.Selectors)
            .Include(s => s.ClassificationRules)
            .Include(s => s.MappingRules)
            .Include(s => s.ParentSelectors)
            .ThenInclude(p => p.ScraperDefinition)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ScraperDefinition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Scrapers
            .Include(s => s.Parameters)
            .Include(s => s.Selectors.Where(z => !z.Disabled))
            .Include(s => s.ClassificationRules.Where(z => !z.Disabled))
            .Include(s => s.MappingRules.Where(z => !z.Disabled))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Add(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Update(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        db.Scrapers.Remove(scraper);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScraperSummary?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await db.Scrapers
            .Include(x => x.MappingRules.Where(x => !x.Disabled))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (scraper is null)
        {
            return null;
        }

        return new ScraperSummary
        {
            Mapping = new MappingSummary
            {
                Title = scraper.MappingRules.Any(x => x.TargetField == TargetField.Title),
                Description = scraper.MappingRules.Any(x => x.TargetField == TargetField.Description),
                Location = scraper.MappingRules.Any(x => x.TargetField == TargetField.Location),
                Venue = scraper.VenueId is not null,
                StartUtc = scraper.MappingRules.Any(x => x.TargetField == TargetField.StartUtc),
                EndUtc = scraper.MappingRules.Any(x => x.TargetField == TargetField.EndUtc),
                MinPrice = scraper.MappingRules.Any(x => x.TargetField == TargetField.MinPrice),
                MaxPrice = scraper.MappingRules.Any(x => x.TargetField == TargetField.MaxPrice),
                DetailsUrl = scraper.MappingRules.Any(x => x.TargetField == TargetField.DetailsUrl),
                ImageUrl = scraper.MappingRules.Any(x => x.TargetField == TargetField.ImageUrl),
                TicketUrl = scraper.MappingRules.Any(x => x.TargetField == TargetField.TicketUrl)
            }
        };
    }
}

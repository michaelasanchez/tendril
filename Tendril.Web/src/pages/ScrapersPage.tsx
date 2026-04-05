import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Table } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button, SquareButton } from '../components/button';
import { Icon } from '../components/Icon';
import { pageStyles, tableStyles } from '../styles';
import type { ScraperDefinition } from '../types/api';

type SortKey =
  | 'name'
  | 'baseUrl'
  | 'state'
  | 'lastSuccessUtc'
  | 'lastFailureUtc';

type SortDirection = 'asc' | 'desc';

interface ScrapersPageProps {
  authorized: boolean;
  authLoading: boolean;
}

interface Sort {
  key: SortKey;
  direction: SortDirection;
}

const sortScraper = (scrapers: ScraperDefinition[], sort: Sort | null) =>
  scrapers.sort((a, b) => {
    if (!sort) return 0;

    const aValue = a[sort.key];
    const bValue = b[sort.key];

    // 3. Handle checking for null/undefined values (optional but recommended)
    if (aValue == null) return 1;
    if (bValue == null) return -1;

    // 4. Compare based on type
    // If it's a string, use localeCompare for accurate text sorting
    if (sort.key === 'lastSuccessUtc' || sort.key === 'lastFailureUtc') {
      // Convert to timestamp (number)
      const aTime = new Date(aValue).getTime();
      const bTime = new Date(bValue).getTime();

      // Handle invalid dates (optional safety check)
      if (isNaN(aTime)) return 1;
      if (isNaN(bTime)) return -1;

      return sort.direction === 'asc' ? aTime - bTime : bTime - aTime;
    }

    if (typeof aValue === 'string' && typeof bValue === 'string') {
      return sort.direction === 'asc'
        ? aValue.localeCompare(bValue)
        : bValue.localeCompare(aValue);
    }

    // Default comparison (numbers, dates, booleans)
    if (aValue < bValue) {
      return sort.direction === 'asc' ? -1 : 1;
    }
    if (aValue > bValue) {
      return sort.direction === 'asc' ? 1 : -1;
    }
    return 0;
  });

export const ScrapersPage: React.FC<ScrapersPageProps> = ({
  authorized,
  authLoading,
}) => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasLoaded = useRef(false);

  const [sort, setSort] = useState<Sort | null>({
    key: 'name',
    direction: 'asc',
  });

  const onSort = (key: SortKey) => {
    setSort((prev) => {
      if (!prev || prev.key !== key) {
        return { key, direction: 'asc' };
      }

      return {
        key,
        direction: prev.direction === 'asc' ? 'desc' : 'asc',
      };
    });
  };

  const sortIndicator = (key: SortKey) =>
    sort?.key === key ? <>&nbsp;{sort.direction === 'asc' ? '▲' : '▼'}</> : '';

  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await ScrapersApi.getAll();
      setScrapers(data);
    } catch (e: any) {
      setError(e.message ?? 'Error loading scrapers.');
    } finally {
      setLoading(false);
    }
  };

  // Initial load
  useEffect(() => {
    if (hasLoaded.current) return;

    if (!authLoading && authorized) {
      hasLoaded.current = true;
      void load();
    }
  }, [authLoading, authorized]);

  // Re-sort table
  useEffect(() => {
    if (!sort) return;
    setScrapers((prev) => {
      const sorted = [...prev].sort((a, b) => {
        var aValue = a[sort.key];
        var bValue = b[sort.key];

        if (aValue === null || aValue === undefined) return 1;
        if (bValue === null || bValue === undefined) return -1;

        if (aValue < bValue) return sort.direction === 'asc' ? -1 : 1;
        if (aValue > bValue) return sort.direction === 'asc' ? 1 : -1;

        return 0;
      });

      return sorted;
    });
  }, [sort]);

  const handleRunNow = async (id: string) => {
    if (!window.confirm('Run this scraper now?')) return;
    try {
      await ScrapersApi.runNow(id);
      await load();
    } catch (e: any) {
      alert(e.message ?? 'Run failed.');
    }
  };

  const scraperGroups = useMemo(() => {
    // 1. Create temporary buckets
    const feedList: ScraperDefinition[] = [];
    const otherList: ScraperDefinition[] = [];
    // const strategyMap = new Map<string, ScraperDefinition[]>();
    // const unknownList: ScraperDefinition[] = [];
    const disabledList: ScraperDefinition[] = [];

    // 2. Sort items into buckets (Single pass)
    scrapers.forEach((scraper) => {
      if (scraper.disabled) {
        disabledList.push(scraper);
        return;
      }

      if (scraper.isEventFeed) {
        feedList.push(scraper);
        return;
      }

      otherList.push(scraper);
    });

    // 4. Construct Final List (Strategies -> Unknown -> Disabled)
    const result = [
      { key: 'Event Feeds', scrapers: sortScraper(feedList, sort) },
    ];

    if (otherList.length > 0) {
      result.push({ key: 'Component', scrapers: sortScraper(otherList, sort) });
    }

    if (disabledList.length > 0) {
      result.push({
        key: 'Disabled',
        scrapers: sortScraper(disabledList, sort),
      });
    }

    return result;
  }, [scrapers]);

  if (authLoading) return <div>Checking session...</div>;
  if (!authorized) return <></>;

  return (
    <section>
      <>
        <div className={pageStyles.pageHeader}>
          <h2>Scrapers</h2>
          <SquareButton onClick={() => navigate('/scrapers/new')}>
            New Scraper
          </SquareButton>
        </div>
        {loading && <p>Loading…</p>}
        {error && <p className="error">{error}</p>}
        <Table className="data-table" hover responsive>
          <thead>
            <tr>
              <th onClick={() => onSort('name')}>
                Name{sortIndicator('name')}
              </th>
              <th onClick={() => onSort('baseUrl')}>
                Base URL{sortIndicator('baseUrl')}
              </th>
              <th onClick={() => onSort('state')}>
                State{sortIndicator('state')}
              </th>
              <th onClick={() => onSort('lastSuccessUtc')}>
                Last Success{sortIndicator('lastSuccessUtc')}
              </th>
              <th onClick={() => onSort('lastFailureUtc')}>
                Last Failure{sortIndicator('lastFailureUtc')}
              </th>
              <th />
            </tr>
          </thead>

          {scraperGroups.map(({ key, scrapers }) => (
            <tbody key={key}>
              <tr className={tableStyles.GroupHeader}>
                <td colSpan={7}>{key}</td>
              </tr>

              {scrapers.map((s) => (
                <tr key={s.id}>
                  <td>{s.name}</td>
                  <td>
                    <a href={s.baseUrl} target="_blank">
                      {s.baseUrl}
                    </a>
                  </td>
                  <td>{s.isEventFeed ? s.state : '-'}</td>
                  <td>
                    {s.lastSuccessUtc ? formatDate(s.lastSuccessUtc) : '-'}
                  </td>
                  <td>
                    {s.lastFailureUtc ? formatDate(s.lastFailureUtc) : '-'}
                  </td>
                  <td className={tableStyles.TableActions}>
                    <div>
                      <Button onClick={() => navigate(`/scrapers/${s.id}`)}>
                        <Icon name="edit" />
                      </Button>
                      {s.isEventFeed && (
                        <Button
                          variant="outline-primary"
                          onClick={() => handleRunNow(s.id)}
                        >
                          <Icon name="run" />
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {scrapers.length === 0 && !loading && (
                <tr>
                  <td colSpan={6}>No scrapers defined yet.</td>
                </tr>
              )}
            </tbody>
          ))}
        </Table>
      </>
    </section>
  );

  function formatDate(dateString: string) {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
};

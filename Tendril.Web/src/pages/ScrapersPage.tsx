import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Table } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { ScrapersApi } from '../api/scrapers';
import { AdminButton as Button } from '../components/button';
import { buttonStyles, pageStyles, tableStyles } from '../styles';
import type { ScraperDefinition } from '../types/api';

type SortKey =
  | 'name'
  | 'baseUrl'
  | 'state'
  | 'lastSuccessUtc'
  | 'lastFailureUtc';

type SortDirection = 'asc' | 'desc';

export const ScrapersPage: React.FC = () => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [sort, setSort] = useState<{
    key: SortKey;
    direction: SortDirection;
  } | null>(null);

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

  const hasLoaded = useRef(false);

  useEffect(() => {
    if (hasLoaded.current) return;

    hasLoaded.current = true;
    void load();
  }, []);

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
    return scrapers.reduce<Record<string, ScraperDefinition[]>>(
      (groups, scraper) => {
        const extractionStrategy = scraper.extractionStrategy ?? 'Unknown';
        if (!groups[extractionStrategy]) {
          groups[extractionStrategy] = [];
        }
        groups[extractionStrategy].push(scraper);
        return groups;
      },
      {}
    );
  }, [scrapers]);

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Scrapers</h2>
        <button
          className={buttonStyles.AdminButton}
          onClick={() => navigate('/scrapers/new')}
        >
          New Scraper
        </button>
      </div>

      {loading && <p>Loading…</p>}
      {error && <p className="error">{error}</p>}

      <Table className="data-table" hover responsive>
        <thead>
          <tr>
            <th onClick={() => onSort('name')}>Name{sortIndicator('name')}</th>
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

        {Object.entries(scraperGroups).map(([strategy, group]) => (
          <tbody key={strategy}>
            <tr className={tableStyles.GroupHeader}>
              <td colSpan={6}>{strategy}</td>
            </tr>

            {group.map((s) => (
              <tr key={s.id}>
                <td>{s.name}</td>
                <td>
                  <a href={s.baseUrl} target="_blank">
                    {s.baseUrl}
                  </a>
                </td>
                <td>{s.state}</td>
                <td>{s.lastSuccessUtc ? formatDate(s.lastSuccessUtc) : '-'}</td>
                <td>{s.lastFailureUtc ? formatDate(s.lastFailureUtc) : '-'}</td>
                <td className={tableStyles.TableActions}>
                  <div>
                    <Button onClick={() => navigate(`/scrapers/${s.id}`)}>
                      Edit
                    </Button>
                    <Button
                      className={buttonStyles.Primary}
                      onClick={() => handleRunNow(s.id)}
                    >
                      Run&nbsp;Now
                    </Button>
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
    </section>
  );

  function formatDate(dateString: string) {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
};

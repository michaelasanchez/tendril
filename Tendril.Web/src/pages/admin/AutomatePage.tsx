// src/pages/ScheduledTasksPage.tsx
import cn from 'classnames';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { ScheduledTasksApi } from '../../api/scheduledTasks';
import { ScrapersApi } from '../../api/scrapers';
import { SquareButton as Button } from '../../components/button';
import { FormCheck, FormInput, FormSelect, FormText } from '../../components/form';
import { Icon } from '../../components/Icon';
import { cardStyles, pageStyles, tableStyles } from '../../styles';
import formStyles from '../../styles/Form.module.css';
import type { Guid, ScheduledTask, ScraperDefinition } from '../../types/api';

type SortKey =
  | 'name'
  | 'cronExpression'
  | 'selectionStrategy'
  | 'status'
  | 'nextRunAtUtc';
type SortDirection = 'asc' | 'desc';

interface Sort {
  key: SortKey;
  direction: SortDirection;
}

export const AutomatePage: React.FC = () => {
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [editing, setEditing] = useState<Partial<ScheduledTask>>({});

  const [isNew, setIsNew] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [showDisabled, setShowDisabled] = useState(true);
  const [sort, setSort] = useState<Sort | null>({
    key: 'name',
    direction: 'asc',
  });

  const hasLoaded = useRef(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [tasksData, scrapersData] = await Promise.all([
        ScheduledTasksApi.getAll(),
        ScrapersApi.getAll(),
      ]);
      setTasks(tasksData);
      setScrapers(scrapersData);
    } catch (e: any) {
      setError(e.message ?? 'Error fetching automation assets.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (hasLoaded.current) return;
    hasLoaded.current = true;
    void load();
  }, []);

  // Client-side Sorting Implementation
  const onSort = (key: SortKey) => {
    setSort((prev) => {
      if (!prev || prev.key !== key) return { key, direction: 'asc' };
      return { key, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
    });
  };

  const sortIndicator = (key: SortKey) =>
    sort?.key === key ? <>&nbsp;{sort.direction === 'asc' ? '▲' : '▼'}</> : '';

  const processedTasks = useMemo(() => {
    const filtered = tasks.filter((t) => showDisabled || !t.isDisabled);
    if (!sort) return filtered;

    return [...filtered].sort((a, b) => {
      let aValue = a[sort.key];
      let bValue = b[sort.key];

      if (aValue == null) return 1;
      if (bValue == null) return -1;

      if (sort.key === 'nextRunAtUtc') {
        const aTime = new Date(aValue).getTime();
        const bTime = new Date(bValue).getTime();
        return sort.direction === 'asc' ? aTime - bTime : bTime - aTime;
      }

      if (typeof aValue === 'string' && typeof bValue === 'string') {
        return sort.direction === 'asc'
          ? aValue.localeCompare(bValue)
          : bValue.localeCompare(aValue);
      }

      if (aValue < bValue) return sort.direction === 'asc' ? -1 : 1;
      if (aValue > bValue) return sort.direction === 'asc' ? 1 : -1;
      return 0;
    });
  }, [tasks, sort, showDisabled]);

  // Inline CRUD Form Invocation
  const startNew = () => {
    setIsNew(true);
    setEditing({
      name: '',
      notes: '',
      isDisabled: false,
      cronExpression: '0 */4 * * *',
      selectionStrategy: 'All',
      scraperIds: [],
    });
    setError(null);
  };

  const startEdit = (task: ScheduledTask) => {
    setIsNew(false);
    setEditing({ ...task });
    setError(null);
  };

  const cancelEdit = () => {
    setEditing({});
    setIsNew(false);
    setError(null);
  };

  const handleScraperToggle = (scraperId: string, isChecked: boolean) => {
    const currentIds = editing.scraperIds ? [...editing.scraperIds] : [];
    const updatedIds = isChecked
      ? [...currentIds, scraperId as Guid]
      : currentIds.filter((id) => id !== scraperId);

    setEditing((prev) => ({ ...prev, scraperIds: updatedIds }));
  };

  const save = async () => {
    if (!editing.name?.trim() || !editing.cronExpression?.trim()) {
      setError('Task Name and Cron Expression are required.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      if (isNew) {
        await ScheduledTasksApi.create({
          name: editing.name,
          notes: editing.notes,
          isDisabled: editing.isDisabled ?? false,
          cronExpression: editing.cronExpression,
          selectionStrategy: editing.selectionStrategy as 'All' | 'Selected',
          scraperIds: editing.scraperIds,
        });
      } else if (editing.id) {
        await ScheduledTasksApi.update(editing.id, {
          name: editing.name,
          notes: editing.notes,
          isDisabled: editing.isDisabled,
          cronExpression: editing.cronExpression,
          selectionStrategy: editing.selectionStrategy as 'All' | 'Selected',
          scraperIds: editing.scraperIds,
        });
      }
      await load();
      cancelEdit();
    } catch (e: any) {
      setError(e.message ?? 'Failed to persist scheduled task states.');
    } finally {
      setSaving(false);
    }
  };

  const toggleDisable = async (task: ScheduledTask) => {
    try {
      await ScheduledTasksApi.update(task.id, {
        isDisabled: !task.isDisabled,
      });
      await load();
    } catch (e: any) {
      alert(e.message ?? 'Failed to toggle status.');
    }
  };

  const remove = async (task: ScheduledTask) => {
    if (
      !window.confirm(`Delete completely schedule strategy for "${task.name}"?`)
    )
      return;
    try {
      await ScheduledTasksApi.delete(task.id);
      await load();
      if (editing.id === task.id) cancelEdit();
    } catch (e: any) {
      alert(e.message ?? 'Delete runtime operation rejected.');
    }
  };

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <div>
          <h2>Task Schedules</h2>
        </div>
        <div style={{ display: 'flex', gap: '1em' }}>
          {tasks.some((t) => t.isDisabled) && (
            <Button
              variant="outline-primary"
              onClick={() => setShowDisabled(!showDisabled)}
            >
              <Icon name={showDisabled ? 'visible' : 'invisible'} /> Disabled
              Tasks
            </Button>
          )}
          <Button variant="primary" onClick={startNew}>
            Add Task
          </Button>
        </div>
      </div>

      {error && !editing.name && (
        <p className="error" style={{ color: '#ff6b6b' }}>
          {error}
        </p>
      )}
      {loading && <p>Loading orchestrations...</p>}

      <Card className={cn(cardStyles.BgCard, cardStyles.MarginBottom)}>
        <Card.Body>
          <Table className={tableStyles.Table} hover responsive>
            <thead>
              <tr>
                <th onClick={() => onSort('name')}>
                  Task Name{sortIndicator('name')}
                </th>
                <th onClick={() => onSort('cronExpression')}>
                  Cron Expression{sortIndicator('cronExpression')}
                </th>
                <th onClick={() => onSort('selectionStrategy')}>
                  Strategy{sortIndicator('selectionStrategy')}
                </th>
                <th onClick={() => onSort('status')}>
                  Worker Status{sortIndicator('status')}
                </th>
                <th onClick={() => onSort('nextRunAtUtc')}>
                  Next Run Due{sortIndicator('nextRunAtUtc')}
                </th>
                <th>Scrapers</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {processedTasks.map((t) => (
                <tr
                  key={t.id}
                  className={t.isDisabled ? tableStyles.Disabled : ''}
                >
                  <td>
                    <strong>{t.name}</strong>
                  </td>
                  <td>
                    <code>{t.cronExpression}</code>
                  </td>
                  <td>{t.selectionStrategy}</td>
                  <td>{t.status}</td>
                  <td>
                    {t.nextRunAtUtc
                      ? new Date(t.nextRunAtUtc).toLocaleString()
                      : '-'}
                  </td>
                  <td>
                    {t.selectionStrategy === 'All'
                      ? 'All Active'
                      : `${t.scraperIds?.length ?? 0} Selected`}
                  </td>
                  <td className={tableStyles.TableActions}>
                    <div>
                      <Button onClick={() => startEdit(t)}>
                        <Icon name="edit" />
                      </Button>
                      <Button onClick={() => toggleDisable(t)}>
                        <Icon name={t.isDisabled ? 'disabled' : 'enable'} />
                      </Button>
                      <Button
                        variant="outline-danger"
                        onClick={() => remove(t)}
                      >
                        <Icon name="remove" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {processedTasks.length === 0 && !loading && (
                <tr>
                  <td colSpan={7}>No scheduled tasks defined.</td>
                </tr>
              )}
            </tbody>
          </Table>
        </Card.Body>
      </Card>

      {/* Inline Management Editor Block */}
      {editing.name !== undefined && (
        <>
          <hr />
          <h4 style={{ marginBottom: '1rem' }}>
            {isNew
              ? 'New Schedule Definition'
              : `Edit Schedule: ${editing.name}`}
          </h4>
          {error && (
            <p className="error" style={{ color: '#ff6b6b' }}>
              {error}
            </p>
          )}

          <Card className={cardStyles.BgCard}>
            <Card.Body>
              <Form
                className={formStyles.form}
                onSubmit={(e) => e.preventDefault()}
              >
                <FormInput
                  label="Task Name"
                  value={editing.name ?? ''}
                  autoFocus
                  onChange={(name) => setEditing({ ...editing, name })}
                />

                <div className={formStyles.formGroup}>
                  <FormCheck
                    label="Disabled"
                    checked={editing.isDisabled ?? false}
                    onChange={(isDisabled) =>
                      setEditing({ ...editing, isDisabled })
                    }
                  />
                </div>

                <div className={formStyles.formGroup}>
                  <FormInput
                    label="Cron Expression"
                    value={editing.cronExpression ?? ''}
                    placeholder="e.g. 0 */4 * * *"
                    onChange={(cronExpression) =>
                      setEditing({ ...editing, cronExpression })
                    }
                  />
                  <small
                    className="text-muted"
                    style={{
                      display: 'block',
                      marginTop: '-0.5rem',
                      marginBottom: '1rem',
                    }}
                  >
                    Layout intervals: Minute, Hour, Day, Month, Day-of-Week
                  </small>
                </div>

                <FormSelect
                  label="Selection Strategy"
                  value={editing.selectionStrategy ?? 'All'}
                  options={[
                    { value: 'All', label: 'All Active Scrapers' },
                    { value: 'Selected', label: 'Selected Scrapers' },
                  ]}
                  onChange={(selectionStrategy) =>
                    setEditing({
                      ...editing,
                      selectionStrategy: selectionStrategy as
                        | 'All'
                        | 'Selected',
                      scraperIds:
                        selectionStrategy === 'All' ? [] : editing.scraperIds,
                    })
                  }
                />

                <FormText
                  label="Notes"
                  value={editing.notes ?? ''}
                  onChange={(notes) => setEditing({ ...editing, notes })}
                />

                {/* Scraper Checkbox List Graph (renders conditionally if strategy is 'Selected') */}
                {editing.selectionStrategy === 'Selected' && (
                  <div style={{ marginTop: '1.5rem', marginBottom: '1.5rem' }}>
                    <label
                      className="form-label"
                      style={{ fontWeight: 'bold', marginBottom: '0.5rem' }}
                    >
                      Scraper Targets Assignment
                    </label>
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: '1fr 1fr',
                        gap: '0.75rem',
                      }}
                    >
                      {scrapers.map((scraper) => {
                        const isChecked =
                          editing.scraperIds?.includes(scraper.id) ?? false;
                        return (
                          <div
                            key={scraper.id}
                            style={{
                              padding: '0.5rem 0.75rem',
                              border: '1px solid #333',
                              borderRadius: '4px',
                              background: scraper.disabled
                                ? '#1a1a1a'
                                : 'transparent',
                              opacity: scraper.disabled ? 0.5 : 1,
                            }}
                          >
                            <FormCheck
                              label={`${scraper.name} ${scraper.disabled ? '(Disabled)' : ''}`}
                              checked={isChecked}
                              onChange={(checked: boolean) =>
                                handleScraperToggle(scraper.id, checked)
                              }
                            />
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}

                <div
                  className={formStyles.buttonRow}
                  style={{ marginTop: '1.5rem' }}
                >
                  <Button variant="primary" onClick={save} disabled={saving}>
                    {saving ? 'Saving System States...' : 'Save Configuration'}
                  </Button>
                  <Button onClick={cancelEdit} disabled={saving}>
                    Cancel
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </>
      )}
    </section>
  );
};

export default AutomatePage;

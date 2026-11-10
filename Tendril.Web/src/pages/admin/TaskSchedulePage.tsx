// src/pages/ScheduledTasksPage.tsx
import cn from 'classnames';
import cronstrue from 'cronstrue';
import { CalendarClock, ChevronDown, ChevronRight, Eye, EyeOff, MoreHorizontal, Pencil, Play, Plus, Power, Trash2 } from 'lucide-react';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Accordion, Alert, Badge, Card, Form } from 'react-bootstrap';
import { ScheduledTasksApi } from '../../api/scheduledTasks';
import { ScrapersApi } from '../../api/scrapers';
import { SquareButton as Button } from '../../components/button';
import { FormCheck, FormInput, FormSelect, FormText } from '../../components/form';
import { FormDropdown } from '../../components/form/FormDropdown';
import formStyles from '../../styles/Form.module.css';
import type { Guid, ScheduledTask, ScraperDefinition } from '../../types/api';
import styles from './TaskSchedulePage.module.css';

const cronPresets = [
  { value: '*/15 * * * *', label: 'Every 15 minutes', description: 'Runs at 0, 15, 30, and 45 minutes past each hour.' },
  { value: '0 * * * *', label: 'Hourly', description: 'Runs at the start of every hour.' },
  { value: '0 */4 * * *', label: 'Every 4 hours', description: 'Runs every four hours, starting at midnight.' },
  { value: '0 2 * * *', label: 'Daily at 2:00 AM', description: 'Runs every day at 2:00 AM.' },
  { value: '0 8 * * 1-5', label: 'Weekdays at 8:00 AM', description: 'Runs Monday through Friday at 8:00 AM.' },
  { value: '0 3 * * 0', label: 'Weekly, Sunday at 3:00 AM', description: 'Runs every Sunday at 3:00 AM.' },
] as const;

const cronFieldLabels = ['Minute', 'Hour', 'Day', 'Month', 'Weekday'];

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

export const TaskSchedulePage: React.FC = () => {
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [editing, setEditing] = useState<Partial<ScheduledTask>>({});
  const [savedCronExpression, setSavedCronExpression] = useState<string | null>(null);

  const [isNew, setIsNew] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [showDisabled, setShowDisabled] = useState(true);
  const [expandedTaskIds, setExpandedTaskIds] = useState<Guid[]>([]);
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

  const activeEventFeeds = scrapers.filter((scraper) => scraper.isEventFeed && !scraper.disabled);
  const enabledCount = tasks.filter((task) => !task.isDisabled).length;
  const runningCount = tasks.filter((task) => task.status === 'Running').length;
  const disabledCount = tasks.filter((task) => task.isDisabled).length;
  const currentCron = editing.cronExpression ?? '';
  const matchingPreset = cronPresets.find((preset) => preset.value === currentCron);
  const cronFields = currentCron.trim().split(/\s+/).slice(0, 5);
  const cronDescription = describeCronExpression(currentCron);

  // Inline CRUD Form Invocation
  const startNew = () => {
    setIsNew(true);
    setSavedCronExpression(null);
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
    setSavedCronExpression(task.cronExpression);
    setEditing({ ...task });
    setError(null);
  };

  const cancelEdit = () => {
    setEditing({});
    setSavedCronExpression(null);
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

  const removeScraperFromTask = async (task: ScheduledTask, scraperId: Guid) => {
    try {
      await ScheduledTasksApi.updateScrapers(task.id, { remove: [scraperId] });
      await load();
    } catch (e: any) {
      alert(e.message ?? 'Failed to remove scraper from schedule.');
    }
  };

  const transferScraper = async (
    sourceTask: ScheduledTask,
    destinationTask: ScheduledTask,
    scraperId: Guid,
    mode: 'copy' | 'move',
  ) => {
    try {
      await ScheduledTasksApi.transferScrapers(destinationTask.id, {
        sourceTaskId: sourceTask.id,
        scraperIds: [scraperId],
        mode,
      });
      await load();
    } catch (e: any) {
      alert(e.message ?? `Failed to ${mode} scraper to schedule.`);
    }
  };

  return (
    <main className={styles.Page}>
      <header className={styles.PageHeader}>
        <div>
          <p className={styles.Eyebrow}>AUTOMATION</p>
          <h1>Task schedules</h1>
          <p className={styles.Subtitle}>Control when scraper collections run and see what is coming up next.</p>
        </div>
        <div className={styles.HeaderActions}>
          {tasks.some((t) => t.isDisabled) && (
            <Button
              variant="outline-primary"
              onClick={() => setShowDisabled(!showDisabled)}
            >
              {showDisabled ? <EyeOff size={16} /> : <Eye size={16} />}
              {showDisabled ? 'Hide disabled' : 'Show disabled'}
            </Button>
          )}
          <Button variant="primary" onClick={startNew}>
            <Plus size={16} /> Add schedule
          </Button>
        </div>
      </header>

      {error && !editing.name && <Alert variant="danger">{error}</Alert>}
      {loading && <p className={styles.Loading}>Loading schedules...</p>}

      <section className={styles.OverviewStrip} aria-label="Schedule summary">
        <SummaryMetric icon={<CalendarClock size={17} />} label="Enabled schedules" value={enabledCount} />
        <SummaryMetric icon={<Play size={16} />} label="Running now" value={runningCount} />
        <SummaryMetric icon={<Power size={16} />} label="Disabled" value={disabledCount} />
      </section>

      <section className={styles.TableSection}>
        <div className={styles.TableHeading}>
          <div><h2>All schedules</h2><span>{processedTasks.length} shown</span></div>
          <span className={styles.TableHint}>Select a column heading to sort</span>
        </div>
        <div className={styles.TableWrap}>
          <Accordion
            as="table"
            className={cn('table', 'table-hover', styles.ScheduleTable)}
            alwaysOpen
            activeKey={expandedTaskIds}
            onSelect={(eventKey) =>
              setExpandedTaskIds(
                Array.isArray(eventKey) ? eventKey : eventKey ? [eventKey] : [],
              )
            }
          >
            <thead>
              <tr>
                <th onClick={() => onSort('name')} className={styles.Sortable}>
                  Task Name{sortIndicator('name')}
                </th>
                <th onClick={() => onSort('cronExpression')} className={styles.Sortable}>
                  Cron Expression{sortIndicator('cronExpression')}
                </th>
                <th onClick={() => onSort('selectionStrategy')} className={styles.Sortable}>
                  Strategy{sortIndicator('selectionStrategy')}
                </th>
                <th onClick={() => onSort('status')} className={styles.Sortable}>
                  Worker Status{sortIndicator('status')}
                </th>
                <th onClick={() => onSort('nextRunAtUtc')} className={styles.Sortable}>
                  Next Run Due{sortIndicator('nextRunAtUtc')}
                </th>
                <th>Targets</th>
                <th />
              </tr>
            </thead>
            {processedTasks.map((t) => {
              const isExpanded = expandedTaskIds.includes(t.id);
              const selectedScrapers = (t.scraperIds ?? []).map((scraperId) =>
                scrapers.find((scraper) => scraper.id === scraperId),
              );

              return (
                <Accordion.Item
                  key={t.id}
                  as="tbody"
                  eventKey={t.id}
                  className={styles.ScheduleAccordionItem}
                >
                  <tr className={cn(t.isDisabled && styles.DisabledRow)}>
                    <td>
                      <div className={styles.TaskNameCell}>
                        <Accordion.Button
                          as="button"
                          className={styles.ExpandAction}
                          aria-label={`${isExpanded ? 'Collapse' : 'Expand'} ${t.name} details`}
                        >
                          {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                        </Accordion.Button>
                        <div>
                          <strong className={styles.TaskName}>{t.name}</strong>
                          {t.notes && <small className={styles.TaskNotes}>{t.notes}</small>}
                        </div>
                      </div>
                    </td>
                    <td>
                      <strong className={styles.CronDescription}>{describeCron(t.cronExpression)}</strong>
                      <code className={styles.CronCode}>{t.cronExpression}</code>
                    </td>
                    <td><Badge bg="light" text="dark" className={styles.Badge}>{t.selectionStrategy === 'All' ? 'All active' : 'Selected'}</Badge></td>
                    <td><TaskStatus status={t.status} disabled={t.isDisabled} /></td>
                    <td>
                      <strong className={styles.NextRun}>{formatDateTime(t.nextRunAtUtc)}</strong>
                    </td>
                    <td>
                      <span className={styles.TargetCount}>
                        {t.selectionStrategy === 'All'
                          ? `${activeEventFeeds.length} feeds`
                          : `${t.scraperIds?.length ?? 0} selected`}
                      </span>
                    </td>
                    <td className={styles.TableActions}>
                      <button type="button" className={styles.IconAction} onClick={() => startEdit(t)} title={`Edit ${t.name}`} aria-label={`Edit ${t.name}`}>
                        <Pencil size={15} />
                      </button>
                      <button type="button" className={styles.IconAction} onClick={() => toggleDisable(t)} title={t.isDisabled ? `Enable ${t.name}` : `Disable ${t.name}`} aria-label={t.isDisabled ? `Enable ${t.name}` : `Disable ${t.name}`}>
                        <Power size={15} />
                      </button>
                      <button type="button" className={cn(styles.IconAction, styles.DangerAction)} onClick={() => remove(t)} title={`Delete ${t.name}`} aria-label={`Delete ${t.name}`}>
                        <Trash2 size={15} />
                      </button>
                    </td>
                  </tr>

                  {isExpanded && (
                    <tr className={styles.ExpandedRow}>
                      <td colSpan={7}>
                        {t.selectionStrategy === 'Selected' ? (
                          <div className={styles.AssignedScrapers}>
                            {selectedScrapers.length > 0 ? selectedScrapers.map((scraper, index) => (
                              <div className={styles.AssignedScraper} key={t.scraperIds[index]}>
                                <span>{scraper?.name ?? `Unknown scraper (${t.scraperIds[index]})`}</span>
                                <FormDropdown
                                  id={`schedule-${t.id}-scraper-${t.scraperIds[index]}-actions`}
                                  title={<MoreHorizontal size={17} />}
                                  options={[
                                    ...tasks
                                      .filter((destination) =>
                                        destination.id !== t.id &&
                                        destination.selectionStrategy === 'Selected',
                                      )
                                      .flatMap((destination) => [
                                        {
                                          value: `move-${destination.id}`,
                                          label: `Move to ${destination.name}`,
                                          action: () => transferScraper(
                                            t,
                                            destination,
                                            t.scraperIds[index],
                                            'move',
                                          ),
                                        },
                                        {
                                          value: `copy-${destination.id}`,
                                          label: `Copy to ${destination.name}`,
                                          action: () => transferScraper(
                                            t,
                                            destination,
                                            t.scraperIds[index],
                                            'copy',
                                          ),
                                        },
                                      ]),
                                    {
                                      value: 'remove',
                                      label: 'Remove from schedule',
                                      action: () => removeScraperFromTask(t, t.scraperIds[index]),
                                    },
                                  ]}
                                />
                              </div>
                            )) : <span className={styles.NoAssignedScrapers}>No scrapers selected.</span>}
                          </div>
                        ) : (
                          <span className={styles.AllTargets}>All active event feeds are included.</span>
                        )}
                      </td>
                    </tr>
                  )}
                </Accordion.Item>
              );
            })}
            {processedTasks.length === 0 && !loading && (
              <tbody>
                <tr>
                  <td colSpan={7} className={styles.Empty}>No schedules match this view.</td>
                </tr>
              </tbody>
            )}
          </Accordion>
        </div>
      </section>

      {/* Inline Management Editor Block */}
      {editing.name !== undefined && (
        <section className={styles.EditorSection}>
          <div className={styles.EditorHeading}>
            <div>
              <p className={styles.Eyebrow}>SCHEDULE CONFIGURATION</p>
              <h2>
                {isNew
                  ? 'New schedule'
                  : `Edit ${editing.name}`}
              </h2>
              <p>Times are interpreted as UTC by the worker.</p>
            </div>
          </div>
          {error && (
            <Alert variant="danger">{error}</Alert>
          )}

          <Card className={styles.EditorCard}>
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

                <div className={cn(formStyles.formGroup, styles.CronEditor)}>
                  <FormSelect
                    label="Schedule pattern"
                    value={matchingPreset?.value ?? 'custom'}
                    options={[
                      ...cronPresets.map((preset) => ({ value: preset.value, label: preset.label })),
                      { value: 'custom', label: 'Custom expression' },
                    ]}
                    onChange={(value) => {
                      const preset = cronPresets.find((candidate) => candidate.value === value);
                      if (preset) {
                        setEditing({ ...editing, cronExpression: preset.value });
                      }
                    }}
                  />

                  <Form.Group>
                    <Form.Label>Cron expression</Form.Label>
                    <Form.Control
                      value={currentCron}
                      placeholder="e.g. 0 */4 * * *"
                      onChange={(event) =>
                        setEditing({ ...editing, cronExpression: event.target.value })
                      }
                    />
                  </Form.Group>
                  <div className={styles.SavedCron}>
                    <span>Currently saved</span>
                    <code>{savedCronExpression ?? 'Not saved yet'}</code>
                  </div>
                  <div className={styles.CronFieldGrid} aria-label="Cron expression fields">
                    {cronFieldLabels.map((label, index) => (
                      <div className={styles.CronField} key={label}>
                        <span>{label}</span>
                        <code>{cronFields[index] ?? '*'}</code>
                      </div>
                    ))}
                  </div>
                  <div className={styles.CronHelp}>
                    <span className={styles.PreviewLabel}>Schedule preview</span>
                    <strong className={cronDescription.valid ? undefined : styles.InvalidCron}>
                      {cronDescription.description}
                    </strong>
                    <span>{matchingPreset?.description ?? 'Five fields: minute, hour, day of month, month, and day of week.'}</span>
                    <small><code>*</code> any value <code>*/n</code> interval <code>1-5</code> range</small>
                  </div>
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

                {editing.selectionStrategy === 'Selected' && (
                  <div className={styles.TargetPicker}>
                    <label className="form-label">Active event feed targets</label>
                    {activeEventFeeds.length > 0 ? (
                      <div className={styles.TargetGrid}>
                        {activeEventFeeds.map((scraper) => {
                          const isChecked =
                            editing.scraperIds?.includes(scraper.id) ?? false;
                          return (
                            <div className={styles.TargetOption} key={scraper.id}>
                              <FormCheck
                                label={scraper.name}
                                checked={isChecked}
                                onChange={(checked: boolean) =>
                                  handleScraperToggle(scraper.id, checked)
                                }
                              />
                            </div>
                          );
                        })}
                      </div>
                    ) : <p className={styles.NoTargets}>No active event feeds available.</p>}
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
        </section>
      )}
    </main>
  );
};

function SummaryMetric({ icon, label, value }: { icon: React.ReactNode; label: string; value: number }) {
  return (
    <div className={styles.SummaryMetric}>
      <span className={styles.SummaryIcon}>{icon}</span>
      <div><span>{label}</span><strong>{value}</strong></div>
    </div>
  );
}

function TaskStatus({ status, disabled }: { status: string; disabled: boolean }) {
  if (disabled) return <Badge bg="secondary" className={styles.StatusBadge}>Disabled</Badge>;
  const variant = status === 'Running' ? 'success' : status === 'Retrying' ? 'warning' : status === 'Error' || status === 'InvalidCron' ? 'danger' : 'light';
  return <Badge bg={variant} text={variant === 'light' || variant === 'warning' ? 'dark' : undefined} className={styles.StatusBadge}>{status}</Badge>;
}

function describeCron(expression: string): string {
  const preset = cronPresets.find((item) => item.value === expression);
  return preset?.label ?? 'Custom schedule';
}

function describeCronExpression(expression: string): { description: string; valid: boolean } {
  const fields = expression.trim().split(/\s+/);
  if (!expression.trim()) return { description: 'Enter a cron expression to preview it.', valid: false };
  if (fields.length !== 5) return { description: 'Use exactly five cron fields.', valid: false };

  try {
    return {
      description: cronstrue.toString(expression, { use24HourTimeFormat: true }),
      valid: true,
    };
  } catch {
    return { description: 'This expression could not be interpreted.', valid: false };
  }
}

function formatDateTime(value?: string | null): string {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not scheduled' : date.toLocaleString();
}

export default TaskSchedulePage;

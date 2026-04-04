import cn from 'classnames';
import React, { useCallback, useEffect, useState } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { ActionsApi } from '../api/scrapers';
import { TagApi } from '../api/tags';
import { SquareButton as Button } from '../components/button';
import {
  FormCheck,
  FormInput,
  FormSelect,
  type SelectOption,
} from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, pageStyles, tableStyles } from '../styles';
import formStyles from '../styles/Form.module.css';
import type {
  Category,
  ConditionType,
  Guid,
  RuleAssignment,
  ScraperClassificationRule,
  Tag,
} from '../types/api';

interface Props {
  scraperId: Guid;
  categories: Category[];
}

const conditionTypeOptions: SelectOption[] = [
  'Default',
  'Equals',
  'NotEquals',
  'Contains',
  'NotContains',
  'StartsWith',
  'EndsWith',
  'GreaterThan',
  'LessThan',
  'GreaterThanOrEqualTo',
  'LessThanOrEqualTo',
  'RegexMatch',
  'RegexNotMatch',
].map((o) => ({ value: o, label: o }));

export const ClassificationRulesTab: React.FC<Props> = ({
  scraperId,
  categories,
}) => {
  const [rules, setRules] = useState<ScraperClassificationRule[]>([]);
  const [tags, setTags] = useState<Tag[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperClassificationRule>>(
    {},
  );
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const abortController = new AbortController();

    try {
      await Promise.all([
        loadRules(abortController.signal),
        loadTags(abortController.signal),
      ]);
    } catch (err) {
      console.error('Failed to load classification rules data', err);
    }
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  const loadRules = useCallback(async (signal?: AbortSignal) => {
    try {
      if (scraperId !== 'new') {
        const rules = await ActionsApi.getClassificationRules(
          scraperId,
          signal,
        );

        setRules(rules);
      }
    } catch (err) {
      console.error('Failed to fetch classification rules', err);
    }
  }, []);

  const loadTags = useCallback(async (signal?: AbortSignal) => {
    try {
      const result = await TagApi.getAll(signal);

      const sortProp = (t: Tag) => t.name;

      const sorted = result.sort((a, b) =>
        sortProp(a).localeCompare(sortProp(b)),
      );

      setTags(sorted);
    } catch (err) {
      console.error('Failed to fetch tags', err);
    }
  }, []);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      order: 0,
      disabled: false,
      sourceJsonPath: '',
      conditionType: 'Default',
      conditionValue: '',
      assignments: [],
    } as Partial<ScraperClassificationRule>);
  };

  const startEdit = (rule: ScraperClassificationRule) => {
    setIsNew(false);
    setEditing({ ...rule });
  };

  const cancelEdit = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    // if (!editing.sourceJsonPath || (!editing.conditionValue) {
    //   console.error('Source JSON path and condition value are required');
    //   return;
    // }

    if (isNew) {
      await ActionsApi.createClassificationRule(scraperId, {
        order: editing.order ?? 0,
        disabled: editing.disabled ?? false,
        sourceJsonPath: editing.sourceJsonPath ?? '',
        conditionType: editing.conditionType ?? 'Default',
        conditionValue: editing.conditionValue ?? '',
        assignments: editing.assignments ?? [],
      });
    } else if (editing.id) {
      await ActionsApi.updateClassificationRule(scraperId, editing.id, {
        order: editing.order ?? 0,
        disabled: editing.disabled ?? false,
        sourceJsonPath: editing.sourceJsonPath ?? '',
        conditionType: editing.conditionType ?? 'Default',
        conditionValue: editing.conditionValue ?? '',
        assignments: editing.assignments ?? [],
      });
    }
    await load();
    cancelEdit();
  };

  const remove = async (rule: ScraperClassificationRule) => {
    if (!window.confirm(`Delete classification rule?`)) return;
    await ActionsApi.deleteClassificationRule(scraperId, rule.id);
    await load();
  };

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <div>
          <h3>Classification Rules</h3>
        </div>
        <Button variant="primary" onClick={startNew}>
          Add&nbsp;Rule
        </Button>
      </div>
      <Card className={cn(cardStyles.BgCard, cardStyles.MarginBottom)}>
        <Card.Body>
          <Table className={tableStyles.Table} hover responsive>
            <thead>
              <tr>
                <th>Source Path</th>
                <th>Condition Type</th>
                <th>Condition Value</th>
                <th>Categories</th>
                <th>Tags</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rules
                .sort((a, b) => a.order - b.order)
                .map((r) => (
                  <tr
                    key={r.id}
                    className={r.disabled ? tableStyles.Disabled : ''}
                  >
                    <td>{r.sourceJsonPath}</td>
                    <td>{r.conditionType}</td>
                    <td>{r.conditionValue}</td>
                    <td>
                      {r.assignments
                        .filter((a) => a.categoryId)
                        .map(
                          (a) =>
                            categories.find((c) => c.id === a.categoryId)?.name,
                        )
                        .join(', ')}
                    </td>

                    <td>
                      {r.assignments
                        .filter((a) => a.tagId)
                        .map((a) => tags.find((t) => t.id === a.tagId)?.name)
                        .join(', ')}
                    </td>

                    <td className={tableStyles.TableActions}>
                      <div>
                        <Button onClick={() => startEdit(r)}>
                          <Icon name="edit" />
                        </Button>
                        <Button
                          variant="outline-danger"
                          onClick={() => remove(r)}
                        >
                          <Icon name="remove" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              {rules.length === 0 && (
                <tr>
                  <td colSpan={5}>No rules yet.</td>
                </tr>
              )}
            </tbody>
          </Table>
        </Card.Body>
      </Card>

      {editing.sourceJsonPath !== undefined && (
        <>
          <h4>{isNew ? 'New Rule' : 'Edit Rule'}</h4>
          <Card className={cardStyles.BgCard}>
            <Card.Body>
              <Form className={formStyles.form}>
                <FormInput
                  label="Order"
                  type="number"
                  value={editing.order?.toString() ?? '0'}
                  onChange={(order) =>
                    setEditing({ ...editing, order: parseInt(order) })
                  }
                />
                <FormInput
                  label="Source Path"
                  value={editing.sourceJsonPath ?? '}'}
                  onChange={(sourceJsonPath) =>
                    setEditing({ ...editing, sourceJsonPath })
                  }
                />
                <FormSelect
                  label="Condition Type"
                  value={editing.conditionType ?? 'Default'}
                  onChange={(conditionType) =>
                    setEditing({
                      ...editing,
                      conditionType: conditionType as ConditionType,
                    })
                  }
                  options={conditionTypeOptions}
                />
                <FormInput
                  label="Condition Value"
                  value={editing.conditionValue ?? ''}
                  onChange={(conditionValue) =>
                    setEditing({ ...editing, conditionValue })
                  }
                />
                <FormSelect
                  label="Category"
                  value={
                    editing.assignments?.find((a) => a.categoryId)
                      ?.categoryId ?? ''
                  }
                  onChange={(categoryId) => {
                    const assignments =
                      editing.assignments?.filter((a) => !a.categoryId) ?? [];

                    setEditing({
                      ...editing,
                      assignments: [
                        ...assignments,
                        {
                          categoryId: categoryId,
                          tagId: null,
                        } as RuleAssignment,
                      ],
                    });
                  }}
                  options={[
                    { value: '', label: 'None' },
                    ...categories.map((c) => ({
                      value: c.id,
                      label: c.name,
                    })),
                  ]}
                />
                <FormCheck
                  label="Disabled"
                  checked={editing.disabled ?? false}
                  onChange={(disabled) => setEditing({ ...editing, disabled })}
                />
                <div className={formStyles.buttonRow}>
                  <Button variant="primary" onClick={save}>
                    Save
                  </Button>
                  <Button onClick={cancelEdit}>Cancel</Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </>
      )}
    </>
  );
};

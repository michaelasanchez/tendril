import cn from 'classnames';
import React, { useEffect, useState, type JSX } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button } from '../components/button';
import {
  FormCheck,
  FormInput,
  FormInputSelect,
  FormSelect,
  type SelectOption,
} from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, pageStyles, tableStyles } from '../styles';
import formStyles from '../styles/Form.module.css';
import type {
  Guid,
  ScraperMappingRule,
  ScraperSelector,
  TransformType,
} from '../types/api';

interface Props {
  scraperId: Guid;
  selectors: ScraperSelector[];
}

const transformTypeOptions: SelectOption[] = [
  'None',
  'Constant',
  'Trim',
  'RegexExtract',
  'RegexReplace',
  'Split',
  'Combine',
  'ParseDate',
  'ParseTime',
  'ParseExact',
  'ParseLoose',
  'ToLower',
  'ToUpper',
  'Currency',
  'DecodeHtml',
  'SrcSetToUrl',
].map((o) => ({ value: o, label: o }));

const targetFieldOptions: SelectOption[] = [
  'Title',
  'Location',
  'Description',
  'StartUtc',
  'EndUtc',
  'MinPrice',
  'MaxPrice',
  'ImageUrl',
  'DetailsUrl',
  'TicketUrl',
].map((o) => ({ value: o, label: o }));

export const ScraperMappingRulesTab: React.FC<Props> = ({
  scraperId,
  selectors,
}) => {
  const [rules, setRules] = useState<ScraperMappingRule[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperMappingRule>>({});
  const [isNew, setIsNew] = useState(false);

  const [sourceFieldOptions, setSourceFieldOptions] = useState<SelectOption[]>(
    [],
  );

  const load = async () => {
    if (scraperId !== 'new') {
      const rules = await ScrapersApi.getMappingRules(scraperId);

      setRules(rules);
    }
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  useEffect(() => {
    const sourceFields = selectors.map((s) => s.fieldName);
    const ruleTargetFields = rules.map((r) => r.targetField);
    const eventTargetFields = targetFieldOptions.map((o) => o.value);

    const dynamicFields = ruleTargetFields.filter(
      (r) => !eventTargetFields.includes(r),
    );

    setSourceFieldOptions(
      [...sourceFields, ...dynamicFields].map((o) => ({
        value: o,
        label: o,
      })),
    );
  }, [rules, selectors]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      targetField: '',
      sourceField: '',
      combineWithField: '',
      order: rules.filter((r) => !r.disabled)?.length ?? 0,
      transformType: 'None',
      constantValue: '',
      regexPattern: '',
      regexReplacement: '',
      splitDelimiter: '',
      disabled: false,
    } as Partial<ScraperMappingRule>);
  };

  const startEdit = (rule: ScraperMappingRule) => {
    setIsNew(false);
    setEditing({ ...rule });
  };

  const cancelEdit = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.targetField || !editing.transformType) return;

    if (isNew) {
      await ScrapersApi.createMappingRule(scraperId, {
        targetField: editing.targetField,
        sourceField: editing.sourceField ?? '',
        combineWithField: editing.combineWithField ?? null,
        order: editing.order ?? 0,
        transformType: editing.transformType,
        constantValue: editing.constantValue ?? null,
        format: editing.format ?? null,
        regexPattern: editing.regexPattern ?? null,
        regexReplacement: editing.regexReplacement ?? null,
        splitDelimiter: editing.splitDelimiter ?? null,
        disabled: editing.disabled ?? false,
      });
    } else if (editing.id) {
      await ScrapersApi.updateMappingRule(scraperId, editing.id, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField,
        order: editing.order,
        transformType: editing.transformType,
        constantValue: editing.constantValue ?? null,
        format: editing.format ?? null,
        regexPattern: editing.regexPattern ?? null,
        regexReplacement: editing.regexReplacement ?? null,
        splitDelimiter: editing.splitDelimiter ?? null,
        disabled: editing.disabled ?? false,
      });
    }
    await load();
    cancelEdit();
  };

  const remove = async (rule: ScraperMappingRule) => {
    if (!window.confirm(`Delete mapping rule for ${rule.targetField}?`)) return;
    await ScrapersApi.deleteMappingRule(scraperId, rule.id);
    await load();
  };

  const emphasizeDynamicFields = (str: string) => {
    return !sourceFieldOptions.some((o) => o.value === str) &&
      !targetFieldOptions.some((o) => o.value === str) ? (
      <i>{str}</i>
    ) : (
      str
    );
  };

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <div>
          <h3>Mapping Rules</h3>
          <div>
            Remaining:{' '}
            {targetFieldOptions
              .filter((o) => !rules.some((r) => r.targetField === o.value))
              .map((o) => <em key={o.value}>{o.label}</em>)
              .reduce(
                (acc, cur, i) =>
                  acc.length
                    ? [
                        ...acc,
                        <React.Fragment key={`sep-${i}`}>, </React.Fragment>,
                        cur,
                      ]
                    : [cur],
                [] as JSX.Element[],
              )}
          </div>
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
                <th>Target</th>
                <th>Source</th>
                <th>Combine With</th>
                <th>Order</th>
                <th>Transform</th>
                <th>Format</th>
                <th>Constant</th>
                <th>Regex Pattern</th>
                <th>Regex Replacement</th>
                <th>Delimiter</th>
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
                    <td>{emphasizeDynamicFields(r.targetField)}</td>
                    <td>{emphasizeDynamicFields(r.sourceField)}</td>
                    <td>{emphasizeDynamicFields(r.combineWithField ?? '-')}</td>
                    <td>{r.order}</td>
                    <td>{r.transformType}</td>
                    <td>{r.constantValue}</td>
                    <td>{r.format}</td>
                    <td>{r.regexPattern}</td>
                    <td>{r.regexReplacement}</td>
                    <td>{r.splitDelimiter}</td>
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

      {editing.targetField !== undefined && (
        <>
          <h4>{isNew ? 'New Rule' : 'Edit Rule'}</h4>
          <Card className={cardStyles.BgCard}>
            <Card.Body>
              <Form className={formStyles.form}>
                <div className={formStyles.formGroup}>
                  <FormInputSelect
                    label="Target"
                    value={editing.targetField ?? ''}
                    options={targetFieldOptions}
                    clearable
                    autoFocus
                    onChange={(targetField) =>
                      setEditing({ ...editing, targetField })
                    }
                  />
                </div>
                <div className={formStyles.formGroup}>
                  <FormInputSelect
                    label="Source"
                    value={editing.sourceField ?? ''}
                    options={sourceFieldOptions}
                    clearable
                    onChange={(sourceField) =>
                      setEditing({ ...editing, sourceField })
                    }
                  />
                </div>
                <FormInput
                  label="Order"
                  type="number"
                  value={editing.order?.toString() ?? '0'}
                  onChange={(order) =>
                    setEditing({ ...editing, order: parseInt(order) })
                  }
                />
                <FormSelect
                  label="Tranform"
                  value={editing.transformType ?? 'None'}
                  onChange={(transformType) =>
                    setEditing({
                      ...editing,
                      transformType: transformType as TransformType,
                    })
                  }
                  options={transformTypeOptions}
                />
                {editing.transformType === 'Combine' && (
                  <div className={formStyles.formGroup}>
                    <FormInputSelect
                      label="Combine With Field"
                      value={editing.combineWithField ?? ''}
                      options={sourceFieldOptions}
                      clearable
                      onChange={(combineWithField) =>
                        setEditing({ ...editing, combineWithField })
                      }
                    />
                  </div>
                )}
                {editing.transformType === 'Constant' && (
                  <FormInput
                    label="Constant Value"
                    value={editing.constantValue ?? ''}
                    onChange={(constantValue) =>
                      setEditing({
                        ...editing,
                        constantValue,
                      })
                    }
                  />
                )}
                {editing.transformType === 'ParseExact' && (
                  <FormInput
                    label="Format"
                    value={editing.format ?? ''}
                    onChange={(format) =>
                      setEditing({
                        ...editing,
                        format,
                      })
                    }
                  />
                )}
                {(editing.transformType === 'RegexReplace' ||
                  editing.transformType === 'RegexExtract') && (
                  <FormInput
                    label="Regex Pattern"
                    value={editing.regexPattern ?? ''}
                    onChange={(regexPattern) =>
                      setEditing({
                        ...editing,
                        regexPattern,
                      })
                    }
                  />
                )}
                {editing.transformType === 'RegexReplace' && (
                  <FormInput
                    label="Regex Replacement"
                    value={editing.regexReplacement ?? ''}
                    onChange={(regexReplacement) =>
                      setEditing({
                        ...editing,
                        regexReplacement,
                      })
                    }
                  />
                )}
                {editing.transformType === 'Split' && (
                  <FormInput
                    label="Split Delimiter"
                    value={editing.splitDelimiter ?? ''}
                    onChange={(splitDelimiter) =>
                      setEditing({
                        ...editing,
                        splitDelimiter,
                      })
                    }
                  />
                )}
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

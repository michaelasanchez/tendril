import React, { useEffect, useState, type JSX } from "react";
import { Card, Form, Table } from "react-bootstrap";
import { ScrapersApi } from "../api/scrapers";
import { FormInput, FormSelect, type SelectOption } from "../components/form";
import formStyles from "../components/form/Form.module.css";
import type {
  Guid,
  ScraperMappingRule,
  ScraperSelector,
  TransformType,
} from "../types/api";

interface Props {
  scraperId: Guid;
  selectors: ScraperSelector[];
}

const transformTypeOptions: SelectOption[] = [
  "None",
  "Trim",
  "RegexExtract",
  "RegexReplace",
  "Split",
  "Combine",
  "ParseDate",
  "ParseTime",
  "ParseExact",
  "ParseLoose",
  "ToLower",
  "ToUpper",
  "Currency",
  "SrcSetToUrl",
].map((o) => ({ value: o, label: o }));

const targetFieldOptions: SelectOption[] = [
  { value: "", label: "" },
  ...[
    "Title",
    "Description",
    "StartUtc",
    "EndUtc",
    "TicketUrl",
    "Category",
    "ImageUrl",
  ].map((o) => ({ value: o, label: o })),
];

export const ScraperMappingRulesTab: React.FC<Props> = ({
  scraperId,
  selectors,
}) => {
  const [rules, setRules] = useState<ScraperMappingRule[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperMappingRule>>({});
  const [isNew, setIsNew] = useState(false);

  const [sourceFieldOptions, setSourceFieldOptions] = useState<SelectOption[]>(
    []
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
    const next = [
      { value: "", label: "" },
      ...selectors.map((s) => s.fieldName).map((o) => ({ value: o, label: o })),
    ];
    setSourceFieldOptions(next);
  }, [selectors]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      targetField: "",
      sourceField: "",
      combineWithField: "",
      order: rules?.length ?? 0,
      transformType: "None",
      regexPattern: "",
      regexReplacement: "",
      splitDelimiter: "",
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
    console.log("save", editing);

    if (!editing.targetField || !editing.sourceField || !editing.transformType)
      return;

    if (isNew) {
      await ScrapersApi.createMappingRule(scraperId, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField ?? null,
        order: editing.order ?? 0,
        transformType: editing.transformType,
        format: editing.format ?? null,
        regexPattern: editing.regexPattern ?? null,
        regexReplacement: editing.regexReplacement ?? null,
        splitDelimiter: editing.splitDelimiter ?? null,
      });
    } else if (editing.id) {
      await ScrapersApi.updateMappingRule(scraperId, editing.id, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField,
        order: editing.order,
        transformType: editing.transformType,
        format: editing.format ?? null,
        regexPattern: editing.regexPattern ?? null,
        regexReplacement: editing.regexReplacement ?? null,
        splitDelimiter: editing.splitDelimiter ?? null,
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
    <Card>
      <Card.Body>
        <div style={{ display: "flex", justifyContent: "space-between" }}>
          <h3>Mapping Rules</h3>

          <button onClick={startNew}>Add Rule</button>
        </div>
        <div>
          Remaining:{" "}
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
              [] as JSX.Element[]
            )}
        </div>
      </Card.Body>
      <Card.Body>
        <Table className="data-table" hover>
          <thead>
            <tr>
              <th>Target Field</th>
              <th>Source Field</th>
              <th>Combine With</th>
              <th>Order</th>
              <th>Transform</th>
              <th>Format</th>
              <th>Regex Pattern</th>
              <th>Regex Replacement</th>
              <th>Split Delimiter</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rules
              .sort((a, b) => a.order - b.order)
              .map((r) => (
                <tr key={r.id}>
                  <td>{emphasizeDynamicFields(r.targetField)}</td>
                  <td>{emphasizeDynamicFields(r.sourceField)}</td>
                  <td>{emphasizeDynamicFields(r.combineWithField ?? "-")}</td>
                  <td>{r.order}</td>
                  <td>{r.transformType}</td>
                  <td>{r.format}</td>
                  <td>{r.regexPattern}</td>
                  <td>{r.regexReplacement}</td>
                  <td>{r.splitDelimiter}</td>
                  <td>
                    <button onClick={() => startEdit(r)}>Edit</button>
                    <button onClick={() => remove(r)}>Delete</button>
                  </td>
                </tr>
              ))}
            {rules.length === 0 && (
              <tr>
                <td colSpan={5}>No mapping rules yet.</td>
              </tr>
            )}
          </tbody>
        </Table>

        {editing.targetField !== undefined && (
          <div className="edit-panel">
            <h4>{isNew ? "New Mapping Rule" : "Edit Mapping Rule"}</h4>
            <Form className={formStyles.form}>
              <div className={formStyles.formGroup}>
                <FormInput
                  label="Target Field"
                  value={editing.targetField ?? ""}
                  onChange={(targetField) =>
                    setEditing({ ...editing, targetField })
                  }
                />
                <FormSelect
                  label="Event Fields"
                  value={editing.targetField ?? ""}
                  onChange={(targetField) =>
                    setEditing({ ...editing, targetField })
                  }
                  options={targetFieldOptions}
                />
              </div>
              <div className={formStyles.formGroup}>
                <FormInput
                  label="Source Field"
                  value={editing.sourceField ?? ""}
                  onChange={(sourceField) =>
                    setEditing({ ...editing, sourceField })
                  }
                />
                <FormSelect
                  label="Selectors"
                  value={editing.sourceField ?? ""}
                  onChange={(sourceField) =>
                    setEditing({ ...editing, sourceField })
                  }
                  options={sourceFieldOptions}
                />
              </div>
              <FormInput
                label="Order"
                type="number"
                value={editing.order?.toString() ?? "0"}
                onChange={(order) =>
                  setEditing({ ...editing, order: parseInt(order) })
                }
              />

              <FormSelect
                label="Tranform"
                value={editing.transformType ?? "None"}
                onChange={(transformType) =>
                  setEditing({
                    ...editing,
                    transformType: transformType as TransformType,
                  })
                }
                options={transformTypeOptions}
              />

              {editing.transformType === "Combine" && (
                <div className={formStyles.formGroup}>
                  <FormInput
                    label="Combine With Field"
                    value={editing.combineWithField ?? ""}
                    onChange={(combineWithField) =>
                      setEditing({ ...editing, combineWithField })
                    }
                  />
                  <FormSelect
                    label="Selectors"
                    value={editing.combineWithField ?? ""}
                    onChange={(combineWithField) =>
                      setEditing({ ...editing, combineWithField })
                    }
                    options={sourceFieldOptions}
                  />
                </div>
              )}
              {editing.transformType === "ParseExact" && (
                <FormInput
                  label="Format"
                  value={editing.format ?? ""}
                  onChange={(format) =>
                    setEditing({
                      ...editing,
                      format,
                    })
                  }
                />
              )}
              {(editing.transformType === "RegexReplace" ||
                editing.transformType === "RegexExtract") && (
                <FormInput
                  label="Regex Pattern"
                  value={editing.regexPattern ?? ""}
                  onChange={(regexPattern) =>
                    setEditing({
                      ...editing,
                      regexPattern,
                    })
                  }
                />
              )}
              {editing.transformType === "RegexReplace" && (
                <FormInput
                  label="Regex Replacement"
                  value={editing.regexReplacement ?? ""}
                  onChange={(regexReplacement) =>
                    setEditing({
                      ...editing,
                      regexReplacement,
                    })
                  }
                />
              )}
              {editing.transformType === "Split" && (
                <FormInput
                  label="Split Delimiter"
                  value={editing.splitDelimiter ?? ""}
                  onChange={(splitDelimiter) =>
                    setEditing({
                      ...editing,
                      splitDelimiter,
                    })
                  }
                />
              )}
            </Form>
            <div className={formStyles.buttonRow}>
              <button onClick={save}>Save</button>
              <button onClick={cancelEdit}>Cancel</button>
            </div>
          </div>
        )}
      </Card.Body>
    </Card>
  );
};

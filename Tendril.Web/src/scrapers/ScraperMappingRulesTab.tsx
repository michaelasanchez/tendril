import React, { useEffect, useState, type JSX } from "react";
import { ScrapersApi } from "../api/scrapers";
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

const transformTypeOptions: TransformType[] = [
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
];

const targetFieldOptions: string[] = [
  "Title",
  "Description",
  "StartUtc",
  "EndUtc",
  "TicketUrl",
  "Category",
  "ImageUrl",
];

export const ScraperMappingRulesTab: React.FC<Props> = ({
  scraperId,
  selectors,
}) => {
  const [rules, setRules] = useState<ScraperMappingRule[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperMappingRule>>({});
  const [isNew, setIsNew] = useState(false);

  const [sourceFieldOptions, setSourceFieldOptions] = useState<string[]>([]);

  const load = async () => {
    const rules = await ScrapersApi.getMappingRules(scraperId);

    setRules(rules);
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  useEffect(() => {
    setSourceFieldOptions(selectors.map((s) => s.fieldName));
  }, [selectors]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      targetField: targetFieldOptions?.[0] ?? "",
      sourceField: sourceFieldOptions?.[0] ?? "",
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
    return !sourceFieldOptions.includes(str) &&
      !targetFieldOptions.includes(str) ? (
      <i>{str}</i>
    ) : (
      str
    );
  };

  return (
    <div className="card">
      <div className="card-header">
        <h3>Mapping Rules</h3>
        <div>
          Remaining:{" "}
          {targetFieldOptions
            .filter((o) => !rules.some((r) => r.targetField === o))
            .map((o) => <span key={o}>{o}</span>)
            .reduce(
              (acc, cur) => (acc.length ? [...acc, <>, </>, cur] : [cur]),
              [] as JSX.Element[]
            )}
        </div>
        <button onClick={startNew}>Add Rule</button>
      </div>

      <table className="data-table">
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
      </table>

      {editing.targetField !== undefined && (
        <div className="edit-panel">
          <h4>{isNew ? "New Mapping Rule" : "Edit Mapping Rule"}</h4>
          <div className="form-grid">
            <label>
              Target Field
              {/* Freeform input */}
              <input
                type="text"
                value={editing.targetField ?? ""}
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    targetField: e.target.value,
                  })
                }
                placeholder="Enter field name..."
                style={{ marginBottom: "4px" }}
              />
              {/* Or choose from dropdown */}
              <select
                value=""
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    targetField: e.target.value,
                  })
                }
              >
                <option value="" disabled>
                  Select from event fields…
                </option>
                {targetFieldOptions.map((o) => (
                  <option key={o} value={o}>
                    {o}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Source Field
              {/* Freeform input */}
              <input
                type="text"
                value={editing.sourceField ?? ""}
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    sourceField: e.target.value,
                  })
                }
                placeholder="Enter field name..."
                style={{ marginBottom: "4px" }}
              />
              {/* Or choose from dropdown */}
              <select
                value=""
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    sourceField: e.target.value,
                  })
                }
              >
                <option value="" disabled>
                  Select from scraped fields…
                </option>
                {sourceFieldOptions.map((o) => (
                  <option key={o} value={o}>
                    {o}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Order
              <input
                type="number"
                value={editing.order}
                onChange={(e) =>
                  setEditing({ ...editing, order: parseInt(e.target.value) })
                }
              />
            </label>
            <label>
              Transform
              <select
                value={editing.transformType ?? "None"}
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    transformType: e.target.value as TransformType,
                  })
                }
              >
                {transformTypeOptions.map((o) => (
                  <option key={o} value={o}>
                    {o}
                  </option>
                ))}
              </select>
            </label>

            {editing.transformType === "Combine" && (
              <label>
                Combine With Field
                {/* Freeform input */}
                <input
                  type="text"
                  value={editing.combineWithField ?? ""}
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      combineWithField: e.target.value,
                    })
                  }
                  placeholder="Enter field name..."
                  style={{ marginBottom: "4px" }}
                />
                {/* Or choose from dropdown */}
                <select
                  value=""
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      combineWithField: e.target.value,
                    })
                  }
                >
                  <option value="" disabled>
                    Select from scraped fields…
                  </option>
                  {sourceFieldOptions.map((o) => (
                    <option key={o} value={o}>
                      {o}
                    </option>
                  ))}
                </select>
              </label>
            )}
            {editing.transformType === "ParseExact" && (
              <label>
                Format
                <input
                  value={editing.format ?? ""}
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      format: e.target.value,
                    })
                  }
                />
              </label>
            )}
            {(editing.transformType === "RegexReplace" ||
              editing.transformType === "RegexExtract") && (
              <label>
                Regex Pattern
                <input
                  value={editing.regexPattern ?? ""}
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      regexPattern: e.target.value,
                    })
                  }
                />
              </label>
            )}
            {editing.transformType === "RegexReplace" && (
              <label>
                Regex Replacement
                <input
                  value={editing.regexReplacement ?? ""}
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      regexReplacement: e.target.value,
                    })
                  }
                />
              </label>
            )}
            {editing.transformType === "Split" && (
              <label>
                Split Delimiter
                <input
                  value={editing.splitDelimiter ?? ""}
                  onChange={(e) =>
                    setEditing({
                      ...editing,
                      splitDelimiter: e.target.value,
                    })
                  }
                />
              </label>
            )}
          </div>
          <div className="button-row">
            <button onClick={save}>Save</button>
            <button onClick={cancelEdit}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
};

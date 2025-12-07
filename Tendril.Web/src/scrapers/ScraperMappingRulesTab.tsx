import React, { useEffect, useState } from "react";
import { ScrapersApi } from "../api/scrapers";
import type { Guid, ScraperMappingRule } from "../types/api";

interface Props {
  scraperId: Guid;
}

export const ScraperMappingRulesTab: React.FC<Props> = ({ scraperId }) => {
  const [rules, setRules] = useState<ScraperMappingRule[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperMappingRule>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await ScrapersApi.getMappingRules(scraperId);
    setRules(data);
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      targetField: "",
      sourceField: "",
      combineWithField: "",
      transformType: "None",
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
    if (!editing.targetField || !editing.sourceField || !editing.transformType)
      return;

    if (isNew) {
      await ScrapersApi.createMappingRule(scraperId, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField ?? null,
        transformType: editing.transformType,
      });
    } else if (editing.id) {
      await ScrapersApi.updateMappingRule(scraperId, editing.id, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField,
        transformType: editing.transformType,
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

  return (
    <div className="card">
      <div className="card-header">
        <h3>Mapping Rules</h3>
        <button onClick={startNew}>Add Rule</button>
      </div>

      <table className="data-table">
        <thead>
          <tr>
            <th>Target Field</th>
            <th>Source Field</th>
            <th>Combine With</th>
            <th>Transform</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {rules.map((r) => (
            <tr key={r.id}>
              <td>{r.targetField}</td>
              <td>{r.sourceField}</td>
              <td>{r.combineWithField ?? "-"}</td>
              <td>{r.transformType}</td>
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
              <input
                value={editing.targetField ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, targetField: e.target.value })
                }
              />
            </label>
            <label>
              Source Field
              <input
                value={editing.sourceField ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, sourceField: e.target.value })
                }
              />
            </label>
            <label>
              Combine With Field
              <input
                value={editing.combineWithField ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, combineWithField: e.target.value })
                }
              />
            </label>
            <label>
              Transform
              <input
                value={editing.transformType ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, transformType: e.target.value })
                }
                placeholder="None, Trim, ParseDate, Currency..."
              />
            </label>
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

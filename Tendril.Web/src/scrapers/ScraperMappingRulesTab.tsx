// src/scrapers/ScraperMappingRulesTab.tsx
import React, { useEffect, useState } from "react";
import type { Guid, ScraperMappingRule } from "../types/api";
import { ScrapersApi } from "../api/scrapers";

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
      transform: "None"
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
    if (!editing.targetField || !editing.sourceField || !editing.transform) return;

    if (isNew) {
      await ScrapersApi.createMappingRule(scraperId, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField ?? null,
        transform: editing.transform
      });
    } else if (editing.id) {
      await ScrapersApi.updateMappingRule(scraperId, editing.id, {
        targetField: editing.targetField,
        sourceField: editing.sourceField,
        combineWithField: editing.combineWithField,
        transform: editing.transform
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
          {rules.map(r => (
            <tr key={r.id}>
              <td>{r.targetField}</td>
              <td>{r.sourceField}</td>
              <td>{r.combineWithField ?? "-"}</td>
              <td>{r.transform}</td>
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

      {(editing.targetField !== undefined) && (
        <div className="edit-panel">
          <h4>{isNew ? "New Mapping Rule" : "Edit Mapping Rule"}</h4>
          <div className="form-grid">
            <label>
              Target Field
              <input
                value={editing.targetField ?? ""}
                onChange={e => setEditing({ ...editing, targetField: e.target.value })}
              />
            </label>
            <label>
              Source Field
              <input
                value={editing.sourceField ?? ""}
                onChange={e => setEditing({ ...editing, sourceField: e.target.value })}
              />
            </label>
            <label>
              Combine With Field
              <input
                value={editing.combineWithField ?? ""}
                onChange={e => setEditing({ ...editing, combineWithField: e.target.value })}
              />
            </label>
            <label>
              Transform
              <input
                value={editing.transform ?? ""}
                onChange={e => setEditing({ ...editing, transform: e.target.value })}
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

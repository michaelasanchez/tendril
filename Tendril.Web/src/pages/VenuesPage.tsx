// src/pages/VenuesPage.tsx
import React, { useEffect, useState } from "react";
import { Form, Table } from "react-bootstrap";
import { VenuesApi } from "../api/venues";
import { FormInput } from "../components/form";
import formStyles from "../components/form/Form.module.css";
import type { Venue } from "../types/api";
import pageStyles from "./Page.module.css";

export const VenuesPage: React.FC = () => {
  const [venues, setVenues] = useState<Venue[]>([]);
  const [editing, setEditing] = useState<Partial<Venue>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await VenuesApi.getAll();
    setVenues(
      data.sort((a, b) =>
        a.name.replace("The ", "").localeCompare(b.name.replace("The ", ""))
      )
    );
  };

  useEffect(() => {
    void load();
  }, []);

  const startNew = () => {
    setIsNew(true);
    setEditing({ name: "", address: "", website: "" });
  };

  const startEdit = (v: Venue) => {
    setIsNew(false);
    setEditing({ ...v });
  };

  const cancel = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.name || !editing.address) return;
    if (isNew) {
      await VenuesApi.create({
        name: editing.name,
        address: editing.address,
        website: editing.website,
      });
    } else if (editing.id) {
      await VenuesApi.update(editing.id, {
        name: editing.name,
        address: editing.address,
        website: editing.website,
      });
    }
    await load();
    cancel();
  };

  const remove = async (v: Venue) => {
    if (!window.confirm(`Delete venue "${v.name}"?`)) return;
    await VenuesApi.delete(v.id);
    await load();
  };

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Venues</h2>
        <button onClick={startNew}>New Venue</button>
      </div>

      <Table className="data-table" hover>
        <thead>
          <tr>
            <th>Name</th>
            <th>Address</th>
            <th>Website</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {venues.map((v) => (
            <tr key={v.id}>
              <td>{v.name}</td>
              <td>{v.address}</td>
              <td>
                {v.website ? (
                  <a href={v.website} target="_blank" rel="noreferrer">
                    {v.website}
                  </a>
                ) : (
                  "-"
                )}
              </td>
              <td>
                <button onClick={() => startEdit(v)}>Edit</button>
                <button onClick={() => remove(v)}>Delete</button>
              </td>
            </tr>
          ))}
          {venues.length === 0 && (
            <tr>
              <td colSpan={4}>No venues yet.</td>
            </tr>
          )}
        </tbody>
      </Table>

      {editing.name !== undefined && (
        <div className="card edit-panel">
          <h3>{isNew ? "New Venue" : "Edit Venue"}</h3>
          <Form className={formStyles.form}>
            <FormInput
              label="Name"
              value={editing.name ?? ""}
              onChange={(name) => setEditing({ ...editing, name })}
            />
            <FormInput
              label="Address"
              value={editing.address ?? ""}
              onChange={(address) => setEditing({ ...editing, address })}
            />
            <FormInput
              label="Website"
              value={editing.website ?? ""}
              onChange={(website) => setEditing({ ...editing, website })}
            />
          </Form>
          <div className={formStyles.buttonRow}>
            <button onClick={save}>Save</button>
            <button onClick={cancel}>Cancel</button>
          </div>
        </div>
      )}
    </section>
  );
};

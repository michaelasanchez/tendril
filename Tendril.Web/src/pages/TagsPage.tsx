// src/pages/TagsPage.tsx
import React, { useEffect, useState } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { TagsApi } from '../api/tags';
import { SquareButton as Button } from '../components/button';
import { FormInput } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, formStyles, pageStyles, tableStyles } from '../styles';
import type { Tag } from '../types/api';

export const TagsPage: React.FC = () => {
  const [Tags, setTags] = useState<Tag[]>([]);
  const [editing, setEditing] = useState<Partial<Tag>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await TagsApi.getAll();
    setTags(
      data.sort((a, b) =>
        a.name.replace('The ', '').localeCompare(b.name.replace('The ', '')),
      ),
    );
  };

  useEffect(() => {
    void load();
  }, []);

  const startNew = () => {
    setIsNew(true);
    setEditing({ name: '' });
  };

  const startEdit = (v: Tag) => {
    setIsNew(false);
    setEditing({ ...v });
  };

  const cancel = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.name) return;
    if (isNew) {
      await TagsApi.create({
        name: editing.name,
      });
    } else if (editing.id) {
      await TagsApi.update(editing.id, {
        name: editing.name,
      });
    }
    await load();
    cancel();
  };

  const remove = async (v: Tag) => {
    if (!window.confirm(`Delete tag "${v.name}"?`)) return;
    await TagsApi.delete(v.id);
    await load();
  };

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Tags</h2>
        <Button onClick={startNew}>New Tag</Button>
      </div>

      <Table className={tableStyles.Table} hover responsive>
        <thead>
          <tr>
            <th>Name</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {Tags.map((v) => (
            <tr key={v.id}>
              <td>{v.name}</td>
              <td className={tableStyles.TableActions}>
                <div>
                  <Button onClick={() => startEdit(v)}>
                    <Icon name="edit" />
                  </Button>
                  <Button onClick={() => remove(v)}>
                    <Icon name="remove" />
                  </Button>
                </div>
              </td>
            </tr>
          ))}
          {Tags.length === 0 && (
            <tr>
              <td colSpan={4}>No Tags yet.</td>
            </tr>
          )}
        </tbody>
      </Table>

      {editing.name !== undefined && (
        <Card className={cardStyles.BgCard}>
          <Card.Body>
            <h3>{isNew ? 'New Tag' : 'Edit Tag'}</h3>
            <Form className={formStyles.form}>
              <FormInput
                label="Name"
                autoFocus
                value={editing.name ?? ''}
                onChange={(name) => setEditing({ ...editing, name })}
              />
              <div className={formStyles.buttonRow}>
                <Button variant="primary" onClick={save}>
                  Save
                </Button>
                <Button onClick={cancel}>Cancel</Button>
              </div>
            </Form>
          </Card.Body>
        </Card>
      )}
    </section>
  );
};

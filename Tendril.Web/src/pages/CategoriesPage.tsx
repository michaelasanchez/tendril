// src/pages/CategoriesPage.tsx
import React, { useEffect, useState } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { CategoriesApi } from '../api/categories';
import { SquareButton as Button } from '../components/button';
import { FormInput } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, formStyles, pageStyles, tableStyles } from '../styles';
import type { Category } from '../types/api';

export const CategoriesPage: React.FC = () => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [editing, setEditing] = useState<Partial<Category>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await CategoriesApi.getAll();
    setCategories(
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
    setEditing({ name: '', description: '' });
  };

  const startEdit = (v: Category) => {
    setIsNew(false);
    setEditing({ ...v });
  };

  const cancel = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.name || editing.description) return;
    if (isNew) {
      await CategoriesApi.create({
        name: editing.name,
        description: editing.description ?? '',
      });
    } else if (editing.id) {
      await CategoriesApi.update(editing.id, {
        name: editing.name,
        description: editing.description,
      });
    }
    await load();
    cancel();
  };

  const remove = async (v: Category) => {
    if (!window.confirm(`Delete category "${v.name}"?`)) return;
    await CategoriesApi.delete(v.id);
    await load();
  };

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Categories</h2>
        <Button onClick={startNew}>New Category</Button>
      </div>

      <Table className={tableStyles.Table} hover responsive>
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {categories.map((v) => (
            <tr key={v.id}>
              <td>{v.name}</td>
              <td>{v.description}</td>
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
          {categories.length === 0 && (
            <tr>
              <td colSpan={4}>No categories yet.</td>
            </tr>
          )}
        </tbody>
      </Table>

      {editing.name !== undefined && (
        <Card className={cardStyles.BgCard}>
          <Card.Body>
            <h3>{isNew ? 'New Category' : 'Edit Category'}</h3>
            <Form className={formStyles.form}>
              <FormInput
                label="Name"
                autoFocus
                value={editing.name ?? ''}
                onChange={(name) => setEditing({ ...editing, name })}
              />
              <FormInput
                label="Description"
                value={editing.description ?? ''}
                onChange={(description) =>
                  setEditing({ ...editing, description })
                }
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

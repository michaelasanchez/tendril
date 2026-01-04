import cn from 'classnames';
import type React from 'react';
import { Button, ButtonGroup, Dropdown, Form } from 'react-bootstrap';
import type { FormSelectProps } from '.';
import styles from '../../styles/Form.module.css';

export const FormInputSelect: React.FC<FormSelectProps> = ({
  label,
  value,
  onChange,
  options,
  placeholder,
  autoFocus = false,
  clearable = false,
  disabled = false,
}) => (
  <Form.Group className={styles.FormInputSelect}>
    <Form.Label>{label}</Form.Label>
    <div>
      <Dropdown as={ButtonGroup} className="w-100">
        <Form.Control
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          disabled={disabled}
          autoFocus={autoFocus}
        />

        <Dropdown.Toggle
          variant="outline-secondary"
          className="flex-shrink-0"
        />

        <Dropdown.Menu className="w-100" align="end">
          {options.map((option) => (
            <Dropdown.Item
              eventKey={option.value}
              onClick={() => onChange(option.value)}
            >
              {option.label}
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown>
      {clearable && (
        <Button
          className={cn(styles.ClearButton, !!value && styles.Show)}
          variant="outline-secondary"
          onClick={() => onChange('')}
        >
          x
        </Button>
      )}
    </div>
  </Form.Group>
);

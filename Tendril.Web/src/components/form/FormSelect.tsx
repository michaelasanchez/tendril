import cn from 'classnames';
import type React from 'react';
import { Button, Form } from 'react-bootstrap';
import type { FormSelectProps } from '.';
import styles from '../../styles/Form.module.css';

export const FormSelect: React.FC<FormSelectProps> = ({
  className,
  label,
  value,
  onChange,
  options,
  autoFocus = false,
  clearable = false,
  disabled = false,
  pill = false,
}) => (
  <Form.Group className={cn(styles.FormSelect, pill && styles.Pill, className)}>
    {!!label && <Form.Label>{label}</Form.Label>}
    <Form.Select
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value)}
      onClick={(e) => e.stopPropagation()}
      autoFocus={autoFocus}
      disabled={disabled}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </Form.Select>
    {clearable && (
      <Button
        className={cn(styles.ClearButton, !value && styles.Show)}
        variant="outline-secondary"
        onClick={() => onChange('')}
      >
        x
      </Button>
    )}
  </Form.Group>
);

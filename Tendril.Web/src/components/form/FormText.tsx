import { Form } from 'react-bootstrap';
import type { FormTextProps } from './types';

export const FormText: React.FC<FormTextProps> = ({
  className,
  label,
  value,
  onChange,
  placeholder,
  disabled = false,
  autoFocus = false,
  rows = 3,
}) => (
  <Form.Group className={className}>
    {label && <Form.Label>{label}</Form.Label>}
    <Form.Control
      as="textarea"
      value={value}
      rows={rows}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      autoFocus={autoFocus}
    />
  </Form.Group>
);

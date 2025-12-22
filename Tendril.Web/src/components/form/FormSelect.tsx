import type React from "react";
import { Form } from "react-bootstrap";
import type { FormSelectProps } from ".";

export const FormSelect: React.FC<FormSelectProps> = ({
  label,
  value,
  onChange,
  options,
  disabled = false,
}) => (
  <Form.Group>
    <Form.Label>{label}</Form.Label>
    <Form.Select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </Form.Select>
  </Form.Group>
);

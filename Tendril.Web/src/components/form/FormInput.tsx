import { Form } from "react-bootstrap";
import type { FormInputProps } from "./types";

export const FormInput: React.FC<FormInputProps> = ({
  label,
  value,
  onChange,
  type = "text",
  placeholder,
  disabled = false,
}) => (
  <Form.Group>
    <Form.Label>{label}</Form.Label>
    <Form.Control
      type={type}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
    />
  </Form.Group>
);

import { Form } from "react-bootstrap";
import type { FormInputProps } from "./types";

export const FormInput: React.FC<FormInputProps> = ({
  className,
  label,
  value,
  onChange,
  type = "text",
  placeholder,
  disabled = false,
  autoFocus = false,
}) => (
  <Form.Group>
    {label && <Form.Label>{label}</Form.Label>}
    <Form.Control
      className={className}
      type={type}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      autoFocus={autoFocus}
    />
  </Form.Group>
);

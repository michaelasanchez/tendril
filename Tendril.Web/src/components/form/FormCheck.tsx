import { Form } from "react-bootstrap";
import type { FormCheckProps } from "./types";

export const FormCheck: React.FC<FormCheckProps> = ({
  label,
  checked,
  onChange,
  inline = false,
  disabled = false,
}) => (
  <Form.Group>
    {/* <Form.Label abel>{label}</Form.Label> */}
    <Form.Check
      type="checkbox"
      label={label}
      checked={checked}
      onChange={(e) => onChange(e.target.checked)}
      inline={inline}
      disabled={disabled}
    />
  </Form.Group>
);

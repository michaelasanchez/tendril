import { Form } from 'react-bootstrap';
import type { FormCheckProps } from './types';

export const FormCheck: React.FC<FormCheckProps> = ({
  className,
  label,
  checked,
  onChange,
  inline = false,
  disabled = false,
}) => (
  <Form.Check
    className={className}
    type="checkbox"
    label={label}
    checked={checked}
    onChange={(e) => onChange(e.target.checked)}
    inline={inline}
    disabled={disabled}
  />
);

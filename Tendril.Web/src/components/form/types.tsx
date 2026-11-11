import type { FormCheckType } from "react-bootstrap/esm/FormCheck";

// type CommonFormProps = {
//   className?: string;
//   label?: string;
//   value: string;
//   onChange: (value: string) => void;
//   placeholder?: string;
//   clearable?: boolean;
//   disabled?: boolean;
//   autoFocus?: boolean;
// };

export interface FormInputProps {
  className?: string;
  label?: string;
  value: string;
  onChange: (value: string) => void;
  type?: "text" | "number" | "password" | "email";
  placeholder?: string;
  clearable?: boolean;
  disabled?: boolean;
  autoFocus?: boolean;
}

export interface FormTextProps {
  value: string;
  onChange: (value: string) => void;
  autoFocus?: boolean;
  className?: string;
  disabled?: boolean;
  placeholder?: string;
  label?: string;
  rows?: number;
}

export interface FormCheckProps {
  className?: string;
  label: string;
  checked: boolean;
  onChange?: (checked: boolean) => void;
  type?: FormCheckType;
  disabled?: boolean;
  inline?: boolean;
  readonly?: boolean;
}

export interface FormDateProps {
  className?: string;
  label?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  autoFocus?: boolean;
}

export interface Option {
  value: string;
  label: string;
}

export interface FormSelectProps {
  className?: string;
  label?: string;
  value: string | null;
  onChange: (value: string) => void;
  options: Option[];
  autoFocus?: boolean;
  clearable?: boolean;
  disabled?: boolean;
  placeholder?: string;
  pill?: boolean;
  inline?: boolean;
}
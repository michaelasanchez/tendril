import type { FormCheckType } from "react-bootstrap/esm/FormCheck";

export interface FormInputProps {
  className?: string;
  label?: string;
  value: string;
  onChange: (value: string) => void;
  type?: "text" | "number" | "password" | "email" | "date";
  placeholder?: string;
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
}

export interface SelectOption {
  value: string;
  label: string;
}

export interface FormSelectProps {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  options: SelectOption[];
  autoFocus?: boolean;
  clearable?: boolean;
  disabled?: boolean;
  placeholder?: string;
}
import cn from 'classnames';
import React, { useEffect, useRef } from 'react';
import { Form } from 'react-bootstrap';
import styles from './Form.module.css';

interface FormDateProps {
  label?: string;
  value: string | null | undefined;
  onChange: (val: string) => void;
  className?: string;
  disabled?: boolean;
  inline?: boolean;
}

export const FormDate: React.FC<FormDateProps> = ({
  label,
  value,
  onChange,
  className,
  disabled = false,
  inline = false,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.value = value || '';
    }
  }, [value]);

  return (
    <Form.Group className={cn(inline && styles.Inline, className)}>
      {label && <Form.Label>{label}</Form.Label>}
      <input
        ref={inputRef}
        type="date"
        className="form-control"
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
        // onBlur={(e) => onChange(e.target.value)}
      />
    </Form.Group>
  );
};

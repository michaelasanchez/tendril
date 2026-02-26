import React, { useRef, useEffect } from 'react';
import { Form } from 'react-bootstrap';

interface FormDateProps {
  label?: string;
  value: string | null | undefined;
  onChange: (val: string) => void;
  className?: string;
  disabled?: boolean;
}

export const FormDate: React.FC<FormDateProps> = ({
  label,
  value,
  onChange,
  className,
  disabled = false,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.value = value || '';
    }
  }, [value]);

  return (
    <Form.Group className={className}>
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

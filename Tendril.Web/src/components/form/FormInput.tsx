import cn from 'classnames';
import React, { useEffect, useRef, useState } from 'react';
import { Form } from 'react-bootstrap';
import { SquareButton } from '../button';
import { Icon } from '../Icon';
import styles from './Form.module.css';
import type { FormInputProps } from './types';

export const FormInput: React.FC<FormInputProps> = ({
  className,
  label,
  value: propValue,
  onChange,
  type = 'text',
  placeholder,
  clearable = false,
  disabled = false,
  autoFocus = false,
}) => {
  const [localValue, setLocalValue] = useState(propValue);
  const timerRef = useRef<number | null>(null);

  // Keep local state in sync
  useEffect(() => {
    setLocalValue(propValue);
  }, [propValue]);

  // Debounce logic
  useEffect(() => {
    // Clear previous timer if user types again
    if (timerRef.current) clearTimeout(timerRef.current);

    if (localValue !== propValue) {
      timerRef.current = setTimeout(() => {
        onChange(localValue);
      }, 500);
    }

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [localValue, onChange, propValue]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      if (timerRef.current) clearTimeout(timerRef.current);
      onChange(localValue);
    }
  };

  return (
    <Form.Group className={cn(styles.Clearable, className)}>
      <div>
        {label && <Form.Label>{label}</Form.Label>}
        <Form.Control
          type={type}
          value={localValue}
          onChange={(e) => setLocalValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          autoFocus={autoFocus}
        />
      </div>
      {clearable && localValue && (
        <SquareButton onClick={() => onChange('')}>
          <Icon name="close" />
        </SquareButton>
      )}
    </Form.Group>
  );
};

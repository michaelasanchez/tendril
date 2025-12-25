import cn from "classnames";
import type React from "react";
import {
  Button,
  ButtonGroup,
  Dropdown,
  DropdownButton,
  Form,
} from "react-bootstrap";
import type { FormSelectProps } from ".";
import styles from "./Form.module.css";

export const FormInputSelect: React.FC<FormSelectProps> = ({
  label,
  value,
  onChange,
  options,
  placeholder,
  autoFocus = false,
  clearable = false,
  disabled = false,
}) => (
  <Form.Group className={styles.FormInputSelect}>
    <Form.Label>{label}</Form.Label>
    <div>
      <ButtonGroup>
        <Form.Control
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          disabled={disabled}
          autoFocus={autoFocus}
        />

        <DropdownButton as={ButtonGroup} title="" variant="outline-secondary" dir="start">
          {options.map((option) => (
            <Dropdown.Item
              eventKey={option.value}
              onClick={() => onChange(option.value)}
            >
              {option.label}
            </Dropdown.Item>
          ))}
        </DropdownButton>
      </ButtonGroup>
      {clearable && (
        <Button
          className={cn(styles.ClearButton, !!value && styles.Show)}
          variant="outline-secondary"
          onClick={() => onChange("")}
        >
          x
        </Button>
      )}
    </div>
    {/* <Form.Select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </Form.Select> */}
  </Form.Group>
);

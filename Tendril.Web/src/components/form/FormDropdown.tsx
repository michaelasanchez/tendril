import cn from 'classnames';
import type React from 'react';
import type { JSX } from 'react';
import { Dropdown, Form } from 'react-bootstrap';
import styles from '../../styles/Form.module.css';
import type { Option } from './types';

interface DropdownOption extends Option {
  action: () => void;
}

interface FormDropdownProps {
  id: string;
  options: DropdownOption[];
  title?: string | JSX.Element;
  pill?: boolean;
}

export const FormDropdown: React.FC<FormDropdownProps> = ({
  id,
  options,
  title,
  pill,
}) => {
  return (
    <Form.Group className={cn(pill && styles.Pill)}>
      <Dropdown>
        <Dropdown.Toggle variant="success" id={id}>
          {title}
        </Dropdown.Toggle>

        <Dropdown.Menu>
          {options.map((option) => (
            <Dropdown.Item key={option.value} onClick={option.action}>
              {option.label}
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown>
    </Form.Group>
  );
};

import cn from 'classnames';
import React, { type ReactNode } from 'react';
import { buttonStyles } from '../../styles';

export type Variant = 'primary' | 'danger' | 'outline-danger';

export interface ButtonProps {
  children: ReactNode;
  className?: string;
  disabled?: boolean;
  href?: string;
  target?: string;
  type?: 'button' | 'submit' | 'reset';
  variant?: Variant;
  onClick?: () => void;
}

// type BaseProps = {
//   children: ReactNode;
//   className?: string;
//   variant?: Variant;
// };

// /* Button behavior */
// type ClickButtonProps = {
//   onClick: () => void;:
//   disabled?: boolean;
//   href?: never;
//   target?: never;
//   type?: 'button' | 'submit' | 'reset';
// };

// /* Link behavior */
// type LinkButtonProps = {
//   href: string;
//   target?: string;
//   onClick?: never;
//   type?: never;
// };

// export type ButtonProps =
//   | (BaseProps & ClickButtonProps)
//   | (BaseProps & LinkButtonProps);

export const Button: React.FC<ButtonProps> = ({
  children,
  className,
  disabled,
  href,
  target,
  type = 'button',
  variant,
  onClick,
}) => {
  return !!href ? (
    <a
      className={cn(buttonStyles.Button, getVariantClass(variant), className)}
      href={href}
      target={target}
      type={type}
    >
      {children}
    </a>
  ) : (
    <button
      className={cn(buttonStyles.Button, getVariantClass(variant), className)}
      disabled={disabled}
      type={type}
      onClick={onClick}
    >
      {children}
    </button>
  );
};

function getVariantClass(varint: Variant | undefined) {
  switch (varint) {
    case 'primary':
      return buttonStyles.Primary;
    case 'danger':
      return buttonStyles.Danger;
    case 'outline-danger':
      return buttonStyles.OutlineDanger;
    default:
      return '';
  }
}

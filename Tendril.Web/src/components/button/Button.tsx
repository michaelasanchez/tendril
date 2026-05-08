import cn from 'classnames';
import React, { type ReactNode } from 'react';
import styles from './Button.module.css';

export type Variant =
  | 'default'
  | 'active' // TODO: should we get rid of this?
  | 'primary'
  | 'outline-primary'
  | 'outline-secondary'
  | 'danger'
  | 'outline-danger';

export interface ButtonProps {
  children?: ReactNode;
  className?: string;
  disabled?: boolean;
  href?: string;
  rel?: string;
  size?: 'sm' | 'md' | 'lg';
  target?: string;
  type?: 'button' | 'submit' | 'reset';
  variant?: Variant;
  onClick?: React.MouseEventHandler<HTMLButtonElement>;
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
  rel,
  size = 'md',
  target,
  type = 'button',
  variant = 'default',
  onClick,
}) => {
  return !!href ? (
    <a
      className={cn(styles.Button, getVariantClass(variant), className)}
      href={href}
      rel={rel}
      target={target}
      type={type}
    >
      {children}
    </a>
  ) : (
    <button
      className={cn(
        styles.Button,
        getVariantClass(variant),
        getSizeClass(size),
        className,
      )}
      disabled={disabled}
      type={type}
      onClick={onClick}
    >
      {children}
    </button>
  );
};

function getSizeClass(size: ButtonProps['size']) {
  switch (size) {
    case 'sm':
      return styles.Small;
    case 'lg':
      return styles.Large;
    default:
      return '';
  }
}

function getVariantClass(varint: Variant | undefined) {
  switch (varint) {
    case 'active':
    case 'primary':
      return styles.Primary;
    case 'outline-primary':
      return styles.OutlinePrimary;
    case 'outline-secondary':
      return styles.OutlineSecondary;
    case 'danger':
      return styles.Danger;
    case 'outline-danger':
      return styles.OutlineDanger;
    default:
      return '';
  }
}

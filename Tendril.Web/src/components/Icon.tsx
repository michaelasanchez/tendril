import {
  ArrowLeft,
  ArrowRight,
  Calendar,
  CloudDownload,
  CloudUpload,
  Edit,
  ExternalLink,
  Eye,
  EyeOff,
  Funnel,
  Heart,
  MapPin,
  Moon,
  Play,
  SlidersHorizontal,
  Sun,
  Ticket,
  Trash,
  X,
  Zap,
  ZapOff,
} from 'lucide-react';

const Icons = {
  calendar: Calendar,
  close: X,
  dark: Moon,
  disabled: ZapOff,
  edit: Edit,
  enable: Zap,
  external: ExternalLink,
  favorite: Heart,
  filter: Funnel,
  invisible: EyeOff,
  light: Sun,
  location: MapPin,
  next: ArrowRight,
  previous: ArrowLeft,
  publish: CloudUpload,
  remove: Trash,
  run: Play,
  sliders: SlidersHorizontal,
  ticket: Ticket,
  unpublish: CloudDownload,
  visible: Eye,
};

export type IconName = keyof typeof Icons;

interface IconProps {
  name: IconName;
  size?: number;
  color?: string;
}

export const Icon: React.FC<IconProps> = ({
  name,
  size = 16,
  color = 'currentColor',
}) => {
  const IconComponent = Icons[name];
  return IconComponent ? <IconComponent size={size} color={color} /> : null;
};

import { Calendar, Edit, ExternalLink, Funnel, Heart, MapPin, Moon, Play, SlidersHorizontal, Sun, Ticket, X } from "lucide-react";

const Icons = {
  calendar: Calendar,
  close: X,
  dark: Moon,
  edit: Edit,
  external: ExternalLink,
  favorite: Heart,
  filter: Funnel,
  light: Sun,
  location: MapPin,
  run: Play,
  sliders: SlidersHorizontal,
  ticket: Ticket,
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
  color = "currentColor",
}) => {
  const IconComponent = Icons[name];
  return IconComponent ? <IconComponent size={size} color={color} /> : null;
};

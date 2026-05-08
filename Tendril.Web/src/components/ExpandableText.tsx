import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from 'react';
import { Button } from './button';

interface Props {
  children: ReactNode;
}

const ExpandableText: React.FC<Props> = ({ children }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [shouldShowButton, setShouldShowButton] = useState(false);
  const textRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const checkOverflow = () => {
      const element = textRef.current;
      if (element) {
        // Reset clamp briefly to measure full height if necessary,
        // or just compare scroll vs client
        setShouldShowButton(element.scrollHeight > element.clientHeight);
      }
    };

    checkOverflow();
    window.addEventListener('resize', checkOverflow);
    return () => window.removeEventListener('resize', checkOverflow);
  }, [children]); // Re-run if content changes

  const containerStyle: CSSProperties = {
    display: '-webkit-box',
    WebkitLineClamp: isExpanded ? 'initial' : 4,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
    lineHeight: '1.5em',
    /* We set a max-height for the 'collapsed' state so scrollHeight has something to compare to */
    maxHeight: isExpanded ? 'none' : '6em',
  };

  return (
    <div>
      <div ref={textRef} style={containerStyle}>
        {children}
      </div>

      {shouldShowButton && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'end',
            marginTop: '1em',
          }}
        >
          <Button
            size="sm"
            variant="outline-secondary"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            {isExpanded ? 'Show Less' : 'Read More'}
          </Button>
        </div>
      )}
    </div>
  );
};

export default ExpandableText;

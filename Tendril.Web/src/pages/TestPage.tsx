import React, { useEffect, useRef, useState } from 'react';
import { Alert, Card, Form } from 'react-bootstrap';

export const TestPage: React.FC = () => {
  const [date, setDate] = useState<string>('');
  const [logs, setLogs] = useState<string[]>([]);
  const manualRef = useRef<HTMLInputElement>(null);

  // For Test 10: Manual DOM Sync
  useEffect(() => {
    if (manualRef.current) {
      manualRef.current.value = date;
    }
  }, [date]);

  const onChange = (val: string, testName: string) => {
    setDate(val);
    const time = new Date().toLocaleTimeString().split(' ')[0];
    setLogs((prev) =>
      [`[${time}] ${testName}: "${val}"`, ...prev].slice(0, 10),
    );
  };

  return (
    <section className="p-3">
      <Card className="mb-4">
        <Card.Body>
          <h3>iOS Reset Bug Lab</h3>
          <Alert variant="info" className="py-2">
            <strong>Current State:</strong> {date || '(empty string)'}
          </Alert>

          <Form>
            {/* GROUP A: RAW BEHAVIOR (Uncontrolled) */}
            <Form.Group className="mb-3">
              <Form.Label>1. Raw Input (Uncontrolled)</Form.Label>



              <input type="date" />






            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>2. Raw + Bootstrap Class (Uncontrolled)</Form.Label>
              <input type="date" className="form-control" />
            </Form.Group>

            {/* GROUP B: REACT CONTROLLED (The likely fail point) */}
            <Form.Group className="mb-3">
              <Form.Label>3. Raw + Value + OnChange (Controlled)</Form.Label>
              <input
                type="date"
                value={date}
                onChange={(e) => onChange(e.target.value, 'Test 3')}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>4. Test 3 + Bootstrap Class</Form.Label>
              <input
                type="date"
                className="form-control"
                value={date}
                onChange={(e) => onChange(e.target.value, 'Test 4')}
              />
            </Form.Group>

            {/* GROUP C: THE "FIX" ATTEMPTS */}
            <Form.Group className="mb-3">
              <Form.Label>5. React-Bootstrap Form.Control</Form.Label>
              <Form.Control
                type="date"
                value={date}
                onChange={(e) => onChange(e.target.value, 'Test 5')}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>6. Test 5 + onInput (Alternative Event)</Form.Label>
              <Form.Control
                type="date"
                value={date}
                onInput={(e) =>
                  onChange((e.target as HTMLInputElement).value, 'Test 6')
                }
                onChange={(e) => onChange(e.target.value, 'Test 6')}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>7. Forced Remount (Key Prop)</Form.Label>
              <input
                key={date ? 'filled' : 'empty'}
                type="date"
                className="form-control"
                value={date}
                onChange={(e) => onChange(e.target.value, 'Test 7')}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>8. Test 4 + onBlur (Loss of Focus)</Form.Label>
              <input
                type="date"
                className="form-control"
                value={date}
                onChange={(e) => onChange(e.target.value, 'Test 8')}
                onBlur={(e) => onChange(e.target.value, 'Test 8 Blur')}
              />
            </Form.Group>

            {/* GROUP D: THE NUCLEAR OPTIONS */}
            <Form.Group className="mb-3">
              <Form.Label>
                9. DefaultValue Only (Pseudo-Uncontrolled)
              </Form.Label>
              <input
                type="date"
                className="form-control"
                defaultValue={date}
                onBlur={(e) => onChange(e.target.value, 'Test 9')}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>
                10. The Manual Ref Bypass (Ref + UseEffect)
              </Form.Label>



              <input
                ref={manualRef}
                type="date"
                className="form-control"
                onChange={(e) => onChange(e.target.value, 'Test 10')}
              />






              
            </Form.Group>
          </Form>
        </Card.Body>
      </Card>

      <Card>
        <Card.Header>
          <strong>Event Log</strong>
        </Card.Header>
        <Card.Body
          style={{
            height: '200px',
            overflowY: 'auto',
            background: '#333',
            color: '#0f0',
            fontFamily: 'monospace',
            fontSize: '12px',
          }}
        >
          {logs.map((log, i) => (
            <div key={i}>{log}</div>
          ))}
        </Card.Body>
      </Card>
    </section>
  );
};

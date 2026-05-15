import { pageStyles } from '../../styles';

interface Props {}

export const AutomatePage: React.FC<Props> = () => {
  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Automate</h2>
      </div>
    </section>
  );
};

export default AutomatePage;

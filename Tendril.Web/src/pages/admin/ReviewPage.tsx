import { pageStyles } from '../../styles';

interface Props {}

export const ReviewPage: React.FC<Props> = () => {
  return (
    <section>
      <div className={pageStyles.pageHeader}>
        <h2>Review</h2>
      </div>
    </section>
  );
};

export default ReviewPage;
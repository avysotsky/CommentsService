import { CommentText } from './CommentText';
import type { Language, Messages } from '../messages';
import { safeHomePage } from './model';
import type { Comment } from './model';

export function CommentsTable({ comments, labels: t, language }: { comments: Comment[]; labels: Messages; language: Language }) {
  return <div className="table-scroll"><table>
    <thead><tr><th scope="col">{t.author}</th><th scope="col">{t.text}</th><th scope="col">{t.posted}</th></tr></thead>
    <tbody>{comments.map(comment => {
      const home = safeHomePage(comment.homePage);
      return <tr key={comment.id}>
        <td><strong dir="auto">{comment.userName}</strong><span className="author-email" dir="auto">{comment.email}</span>
          {home && <a href={home} target="_blank" rel="noopener noreferrer">{t.website} ↗</a>}</td>
        <td className="comment-text" dir="auto"><CommentText text={comment.text} /></td>
        <td><time dateTime={comment.createdAt}>{new Date(comment.createdAt).toLocaleString(language)}</time></td>
      </tr>;
    })}</tbody>
  </table></div>;
}

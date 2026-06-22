import { useParams } from "react-router";

export function ThreadPage() {
    const { id } = useParams<{ id: string }>();
    return <>This is the thread page for thread with id: {id}</>;
}

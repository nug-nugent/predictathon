import { useParams } from "react-router"

export function ThreadPage() {
  let { id } = useParams<{ id: string }>()
  return (
    <>
      This is the thread page for thread with id: {id}
    </>
  )
}
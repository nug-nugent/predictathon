import { useParams } from "react-router"

export function ProfilePage() {
  let { id } = useParams<{ id: string }>()
  return (
    <>
      This is the profile page for user with id: {id}
    </>
  )
}
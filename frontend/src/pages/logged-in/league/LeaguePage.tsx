import { Center, Spinner, Table } from "@chakra-ui/react"
import { useEffect, useState } from "react"
import { getLeague } from "../../../services/league-service";
import { Link } from "react-router";

export function LeaguePage() {
  const [items, setItems] = useState<any[]>([]);

  useEffect(() => {
    const asyncFetch = async () => {
      const leagueData = await getLeague();
      setItems(leagueData.results);
    }
    asyncFetch();
  }), [];

  return (
    <>
      {items.length === 0 ? (
        <Center mt={4}>
          <Spinner />
          </Center>
      ) : (
      <Table.Root size="sm" variant="line" striped showColumnBorder stickyHeader>
        <Table.ColumnGroup>
          <Table.Column htmlWidth="20px" />
          <Table.Column htmlWidth="50%" />
          <Table.Column />
          <Table.Column />
          <Table.Column />
          <Table.Column />
          <Table.Column />
          <Table.Column />
          <Table.Column />
        </Table.ColumnGroup>
        <Table.Header>
          <Table.Row>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POS</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>NAME</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>3</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>2</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>1</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>0</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"} display={{ base: "none", sm: "table-cell" }}>L</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>POINTS</Table.ColumnHeader>
            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>AGD</Table.ColumnHeader>
          </Table.Row>
        </Table.Header>
        <Table.Body>
          {items.map((item) => (
              
            <Table.Row key={item.pos}>
              <Table.Cell fontSize={"0.9em"} textAlign={"right"}>{item.pos}</Table.Cell>
              <Table.Cell fontSize={"0.9em"}><Link to={`/profile/${item.userId}`}>{item.username}</Link></Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.3"} display={{ base: "none", sm: "table-cell" }}>{item.three}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.2"} display={{ base: "none", sm: "table-cell" }}>{item.two}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.1"} display={{ base: "none", sm: "table-cell" }}>{item.one}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.zero}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.missed}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.points}</Table.Cell>
              <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.agd}</Table.Cell>
            </Table.Row>
              
          ))}
        </Table.Body>
      </Table.Root>
      )}
    </>
  )
}
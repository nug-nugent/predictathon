import { Center, Spinner, Table, Text } from "@chakra-ui/react"
import { useEffect, useState } from "react"
import { getLeagueTable, type LeagueTableItem } from "../../../services/league-service";
import { useCompetition } from "../../../providers/CompetitionProvider";
import { Link } from "react-router";

export function LeaguePage() {
  const { currentCompetitionId, isLoading: competitionLoading } = useCompetition();

  if (competitionLoading) {
    return (
      <Center mt={4}>
        <Spinner />
      </Center>
    );
  }

  if (!currentCompetitionId) {
    return (
      <Center mt={4}>
        <Text>You're not registered for any competitions yet.</Text>
      </Center>
    );
  }

  // Keyed by competitionId so switching competitions remounts this fresh (a new loading spinner,
  // not a flash of the previous competition's stale table) instead of needing to reset state.
  return <LeagueTable key={currentCompetitionId} competitionId={currentCompetitionId} />;
}

function LeagueTable({ competitionId }: { competitionId: string }) {
  const [items, setItems] = useState<LeagueTableItem[] | null>(null);

  useEffect(() => {
    getLeagueTable(competitionId).then(setItems);
  }, [competitionId]);

  if (items === null) {
    return (
      <Center mt={4}>
        <Spinner />
      </Center>
    );
  }

  return (
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

          <Table.Row key={item.userID}>
            <Table.Cell fontSize={"0.9em"} textAlign={"right"}>{item.leaguePosition}</Table.Cell>
            <Table.Cell fontSize={"0.9em"}><Link to={`/profile/${item.userID}`}>{item.username}</Link></Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.3"} display={{ base: "none", sm: "table-cell" }}>{item.threePointers}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.2"} display={{ base: "none", sm: "table-cell" }}>{item.twoPointers}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.1"} display={{ base: "none", sm: "table-cell" }}>{item.onePointers}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPointers}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"} color={"points.0"} display={{ base: "none", sm: "table-cell" }}>{item.noPredictions}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.score}</Table.Cell>
            <Table.Cell fontSize={"0.9em"} textAlign={"center"}>{item.averageGoalDifference}</Table.Cell>
          </Table.Row>

        ))}
      </Table.Body>
    </Table.Root>
  );
}

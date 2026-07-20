import { ButtonGroup, IconButton, Pagination } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";

// The standard pager rendered under a paged table. Renders nothing when everything fits on one
// page, so callers don't need their own count > pageSize guard.
export function TablePagination({ count, pageSize, page, onPageChange }: {
    count: number;
    pageSize: number;
    page: number;
    onPageChange: (page: number) => void;
}) {
    if (count <= pageSize) {
        return null;
    }

    return (
        <Pagination.Root count={count} pageSize={pageSize} page={page} onPageChange={(e) => onPageChange(e.page)}>
            <ButtonGroup variant="ghost" size="sm" justifyContent="center" mt={2}>
                <Pagination.PrevTrigger asChild>
                    <IconButton aria-label="Previous page"><ChevronLeft /></IconButton>
                </Pagination.PrevTrigger>
                <Pagination.Items
                    render={(p) => (
                        <IconButton aria-label={`Page ${p.value}`} variant={{ base: "ghost", _selected: "outline" }} onClick={() => onPageChange(p.value)}>
                            {p.value}
                        </IconButton>
                    )}
                />
                <Pagination.NextTrigger asChild>
                    <IconButton aria-label="Next page"><ChevronRight /></IconButton>
                </Pagination.NextTrigger>
            </ButtonGroup>
        </Pagination.Root>
    );
}

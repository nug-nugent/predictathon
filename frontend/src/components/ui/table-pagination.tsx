import { ButtonGroup, HStack, IconButton, Pagination } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";

// The standard pager rendered under a paged table. Renders nothing when everything fits on one
// page, so callers don't need their own count > pageSize guard.
//
// The full row of page-number buttons doesn't truncate down to a fixed width - with enough pages
// it just keeps growing - so below `md` we swap it for Prev/Next plus a "page X of Y" label to
// avoid the pager stretching wider than the screen.
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
        <Pagination.Root count={count} pageSize={pageSize} page={page} onPageChange={(e) => onPageChange(e.page)} width="full">
            <HStack justifyContent="center" width="full" gap={2} mt={2} hideFrom="md">
                <Pagination.PrevTrigger asChild>
                    <IconButton aria-label="Previous page" variant="ghost" size="sm"><ChevronLeft /></IconButton>
                </Pagination.PrevTrigger>
                <Pagination.PageText format="compact" />
                <Pagination.NextTrigger asChild>
                    <IconButton aria-label="Next page" variant="ghost" size="sm"><ChevronRight /></IconButton>
                </Pagination.NextTrigger>
            </HStack>
            <ButtonGroup variant="ghost" size="sm" justifyContent="center" width="full" mt={2} hideBelow="md">
                <Pagination.PrevTrigger asChild>
                    <IconButton aria-label="Previous page"><ChevronLeft /></IconButton>
                </Pagination.PrevTrigger>
                <Pagination.Items
                    render={(p) => (
                        <IconButton
                            aria-label={`Page ${p.value}`}
                            colorPalette="action"
                            variant={{ base: "ghost", _selected: "solid" }}
                            _selected={{ bg: "pagination.selectedBg", color: "pagination.selectedFg" }}
                            onClick={() => onPageChange(p.value)}
                        >
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

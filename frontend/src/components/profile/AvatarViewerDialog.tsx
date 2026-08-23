import { Button, Dialog, Image, Portal } from "@chakra-ui/react";

/// Shows a player's photo at full size, opened by clicking their avatar on the profile page. The
/// stored image is 400x320, so it's capped at that rather than stretched to the dialog's width.
export function AvatarViewerDialog({ open, onClose, username, imageUrl }: {
    open: boolean;
    onClose: () => void;
    username: string;
    imageUrl: string;
}) {
    return (
        <Dialog.Root open={open} onOpenChange={(e) => { if (!e.open) onClose(); }} size="md">
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>{username}</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <Image src={imageUrl} alt={`${username}'s photo`} w="100%" maxW="400px" mx="auto" rounded="md" />
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button variant="ghost" onClick={onClose}>Close</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

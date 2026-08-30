import { useCallback, useEffect, useRef, useState } from "react";
import { Box, Button, Dialog, HStack, Portal, Slider, Text, VStack } from "@chakra-ui/react";
import Cropper, { type Area } from "react-easy-crop";
import { uploadAvatar, removeAvatar, type UploadedAvatar } from "../../services/profile-service";
import { ApiError } from "../../services/api";

const ASPECT = 10 / 8;

export function AvatarUploadDialog({ open, onClose, hasAvatar, onUploaded, onRemoved }: {
    open: boolean;
    onClose: () => void;
    hasAvatar: boolean;
    onUploaded: (avatar: UploadedAvatar) => void;
    onRemoved: () => void;
}) {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [file, setFile] = useState<File | null>(null);
    const [imageSrc, setImageSrc] = useState<string | null>(null);
    const [crop, setCrop] = useState({ x: 0, y: 0 });
    const [zoom, setZoom] = useState(1);
    const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null);
    const [saving, setSaving] = useState(false);
    const [removing, setRemoving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Revoke the object URL when it's replaced or the dialog unmounts, so we don't leak memory.
    useEffect(() => {
        return () => {
            if (imageSrc) URL.revokeObjectURL(imageSrc);
        };
    }, [imageSrc]);

    const reset = () => {
        setFile(null);
        setImageSrc(null);
        setCrop({ x: 0, y: 0 });
        setZoom(1);
        setCroppedAreaPixels(null);
        setError(null);
        if (fileInputRef.current) fileInputRef.current.value = "";
    };

    const handleClose = () => {
        reset();
        onClose();
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const selected = e.target.files?.[0];
        if (!selected) return;

        if (!selected.type.startsWith("image/")) {
            setError("Please choose an image file.");
            return;
        }

        setError(null);
        setFile(selected);
        setCrop({ x: 0, y: 0 });
        setZoom(1);
        setImageSrc(URL.createObjectURL(selected));
    };

    const onCropComplete = useCallback((_croppedArea: Area, pixels: Area) => {
        setCroppedAreaPixels(pixels);
    }, []);

    const handleSave = async () => {
        if (!file || !croppedAreaPixels) return;

        setSaving(true);
        setError(null);
        try {
            onUploaded(await uploadAvatar(file, croppedAreaPixels));
            handleClose();
        } catch (err) {
            setError(err instanceof ApiError ? err.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    const handleRemove = async () => {
        setRemoving(true);
        setError(null);
        try {
            await removeAvatar();
            onRemoved();
            handleClose();
        } catch (err) {
            setError(err instanceof ApiError ? err.messages.join(" ") : "Something went wrong. Please try again.");
        } finally {
            setRemoving(false);
        }
    };

    const busy = saving || removing;

    return (
        <Dialog.Root open={open} onOpenChange={(e) => { if (!e.open) handleClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>Change photo</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                {!imageSrc ? (
                                    <Button onClick={() => fileInputRef.current?.click()}>Choose a Photo</Button>
                                ) : (
                                    <>
                                        <Box position="relative" h="300px" bg="bg.muted" rounded="md" overflow="hidden">
                                            <Cropper
                                                image={imageSrc}
                                                crop={crop}
                                                zoom={zoom}
                                                aspect={ASPECT}
                                                onCropChange={setCrop}
                                                onZoomChange={setZoom}
                                                onCropComplete={onCropComplete}
                                            />
                                        </Box>
                                        <Slider.Root
                                            value={[zoom]}
                                            min={1}
                                            max={3}
                                            step={0.01}
                                            onValueChange={(e) => setZoom(e.value[0])}
                                        >
                                            <Slider.Control>
                                                <Slider.Track>
                                                    <Slider.Range />
                                                </Slider.Track>
                                                <Slider.Thumb index={0} />
                                            </Slider.Control>
                                        </Slider.Root>
                                        <Button size="xs" variant="ghost" alignSelf="start" onClick={() => fileInputRef.current?.click()}>
                                            Choose a Different Photo
                                        </Button>
                                    </>
                                )}
                                <input ref={fileInputRef} type="file" accept="image/*" hidden onChange={handleFileChange} />
                                {error && <Text fontSize="sm" color="fg.error">{error}</Text>}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer justifyContent={hasAvatar ? "space-between" : "flex-end"}>
                            {hasAvatar && (
                                <Button variant="ghost" colorPalette="red" loading={removing} disabled={busy} onClick={() => { void handleRemove(); }}>
                                    Remove Photo
                                </Button>
                            )}
                            <HStack>
                                <Button variant="ghost" disabled={busy} onClick={handleClose}>Cancel</Button>
                                <Button colorPalette="action" loading={saving} disabled={busy || !croppedAreaPixels} onClick={() => { void handleSave(); }}>
                                    Save
                                </Button>
                            </HStack>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

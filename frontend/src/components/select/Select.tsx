import {
    Select as ChSelect,
    createListCollection,
    Portal,
    type ConditionalValue,
    type ListCollection
} from "@chakra-ui/react";
import { useEffect, useState } from "react";

type SelectItem = { label: string; value: string };

export function Select({
    size = "sm",
    width = "full",
    placeholder = "",
    items,
    value,
    onValueChange,
    allowClear = false
}: {
    size?: ConditionalValue<"xs" | "sm" | "md" | "lg" | undefined>;
    width?: ConditionalValue<string>;
    placeholder?: string;
    items: SelectItem[];
    value?: string;
    onValueChange: (value: string | null) => void;
    allowClear?: boolean;
}) {
    const [collection, setCollection] = useState<ListCollection<SelectItem>>(createListCollection({ items }));
    const [_value, setValue] = useState<string[]>(value ? [value] : []);

    useEffect(() => {
        setCollection(createListCollection({ items }));
    }, [items]);

    useEffect(() => {
        setValue(value ? [value] : []);
    }, [value]);

    const _onValueChange = (values: string[]) => {
        setValue(values);
        onValueChange(values.length ? values[0] : null);
    };

    return (
        <ChSelect.Root
            size={size}
            w={width}
            collection={collection}
            value={_value}
            onValueChange={(e) => _onValueChange(e.value)}
            loopFocus={true}>
            <ChSelect.HiddenSelect />
            <ChSelect.Control>
                <ChSelect.Trigger>
                    <ChSelect.ValueText placeholder={placeholder} />
                </ChSelect.Trigger>
                <ChSelect.IndicatorGroup>
                    {allowClear && <ChSelect.ClearTrigger />}
                    <ChSelect.Indicator />
                </ChSelect.IndicatorGroup>
            </ChSelect.Control>
            <Portal>
                <ChSelect.Positioner>
                    <ChSelect.Content>
                        {items.map((item) => (
                            <ChSelect.Item item={item} key={item.value}>
                                {item.label}
                                <ChSelect.ItemIndicator />
                            </ChSelect.Item>
                        ))}
                    </ChSelect.Content>
                </ChSelect.Positioner>
            </Portal>
        </ChSelect.Root>
    );
}

import React, { useState } from "react";
import { ArrowContainer, Popover } from "react-tiny-popover";
import { Container, PopoverButton, PopoverCloseIcon, PopoverContainer, PopoverHeader, PopoverImage, PopoverImageContainer, PopoverTitle } from "./styles";

export const Reaction = ({ messageId, reactions, onAddReaction, onRemoveReaction }) => {
    const [isPopoverOpen, setPopoverOpen] = useState(false);

    const isMe = reactions.some(function (r) { return r.IsMe; });
    const [reaction] = reactions;

    const onButtonClick = () => {
        isMe ? onRemoveReaction(messageId, reaction.Name) : onAddReaction(messageId, reaction.Name, reaction.Url);
    };

    return (
        <div onMouseEnter={() => setPopoverOpen(true)} onMouseLeave={() => setPopoverOpen(false)}>
            <Popover
                isOpen={isPopoverOpen}
                positions={["bottom"]}
                containerStyle={{ zIndex: 2500 }}
                reposition="false"
                align="center"
                content={({ position, childRect, popoverRect }) => (
                    <ArrowContainer
                        position={position}
                        childRect={childRect}
                        popoverRect={popoverRect}
                        arrowSize={8}
                        arrowStyle={{
                            borderBottom: "8px solid #AAA",
                        }}>
                        <ArrowContainer
                            position={position}
                            childRect={childRect}
                            popoverRect={popoverRect}
                            arrowSize={8}
                            style={{
                                padding: 0,
                            }}
                            arrowStyle={{
                                top: "1px",
                        		borderBottom: "9px solid #FFFFFF",
                            }}>
                            <PopoverContainer>
                                <PopoverHeader>
                                    <PopoverImageContainer>
                                        <PopoverImage src={reaction.Url} />                                            
                                    </PopoverImageContainer>
                                    <PopoverButton onClick={onButtonClick}>{isMe ? "Remove " : "Add "}me</PopoverButton>
                                    <PopoverCloseIcon onClick={() => setPopoverOpen(false)} />
                                </PopoverHeader>
                                <PopoverTitle>{reaction.Name}</PopoverTitle>
                                {reactions.map((r) => r.Username).join(', ')}
                            </PopoverContainer>
                        </ArrowContainer>
                    </ArrowContainer>
                )}>
                <Container isMe={isMe} onClick={() => setPopoverOpen(!isPopoverOpen)}>
                    <img src={reaction.Url} />
                    <span>{reactions.length}</span>
                </Container>
            </Popover>
        </div>
    );
};

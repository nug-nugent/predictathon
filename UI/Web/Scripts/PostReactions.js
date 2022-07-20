(function () {
	$(document).ready(function () {
		$('.ThreadMessageHeader').each(function () {
			var container = $(this);
			renderReactions(container, getReactionData(container));
		});

		if (!emojiButton) {
			console.warn("EmojiButton was not loaded");
			$('.ThreadMessageAddReaction').hide();
			return;
		}

		emojiButton.on('hidden', function () {
			emojiButton.off('emoji');
		});

		$('.ThreadMessageAddReaction').on('click', function (e) {
			emojiButton.on('emoji', function (emoji) {
				addReaction($(e.target).parent(), emoji.name, emoji.url);
			});

			emojiButton.togglePicker(e.target);
		});
	});

	function getReactionData(container) {
		var reactionData = container.data('reactions');

		if (!reactionData) {
			reactionData = JSON.parse(container.children("[id*=hdnReactions]").val());
			container.data('reactions', reactionData);
		}

		return reactionData;
	}

	function renderReactions(container, reactionData) {
		var reactionDivs = container.find('.ThreadMessageReaction').addClass('to-be-removed');

		reactionMap = reactionData.reduce(function (map, reaction) {
			map[reaction.Name] = map[reaction.Name] || [];
			map[reaction.Name].push(reaction);
			return map;
		}, {});

		for (var name in reactionMap) {
			addOrUpdateReactionDiv(container, reactionDivs, reactionMap[name], name);
		}

		var toRemove = container.find('.ThreadMessageReaction.to-be-removed');
		if (toRemove.length > 0) {
			toRemove.data('tippy').hide();
			toRemove.remove();
		}
	}

	function addOrUpdateReactionDiv(container, reactionDivs, reactions, name) {
		var tooltipDiv, isMe = reactions.some(function (r) { return r.IsMe; }),
			url = reactions[reactions.length - 1].Url, // uses the most recent URL
			reactionDiv = reactionDivs.find('img[alt="' + name + '"]').parent();

		if (reactionDiv.length === 0) {
			reactionDiv = $('<div class="ThreadMessageReaction"><img><span></span></div>');

			tooltipDiv = $('<div class="ReactionTooltip"><div class="ReactionTooltipImage"><img></div>' +
				'<div class="ReactionTooltipClose"></div>' +
				'<div class="ReactionTooltipButton"><button></button></div>' +
				'<div class="ReactionTooltipTitle"></div><div class="ReactionTooltipNames"></div></div>');

			reactionDiv.data('tooltipDiv', tooltipDiv);
			var tip = tippy(reactionDiv[0], {
				content: tooltipDiv[0],
				theme: 'light-border',
				interactive: true,
				placement: 'bottom'
			});
			tooltipDiv.find('.ReactionTooltipClose').on('click', function () { tip.hide(); });
			tooltipDiv.find('img').attr('src', url).attr('alt', name);
			tooltipDiv.find('.ReactionTooltipTitle').text(name);

			reactionDiv.data('tippy', tip).find('img').attr('src', url).attr('alt', name);
			container.append(reactionDiv);
		}

		reactionDiv.find('span').text(reactions.length);
		reactionDiv.toggleClass('reacted', isMe).removeClass('to-be-removed');

		tooltipDiv = tooltipDiv || reactionDiv.data('tooltipDiv');
		tooltipDiv.find('.ReactionTooltipNames').text(reactions.map(function (r) { return r.Username; }).join(', '));
		tooltipDiv.find('.ReactionTooltipButton button').text(isMe ? 'Remove me' : 'Add me')
			.off('click').on('click', function () {
				isMe ? removeReaction(container, name) : addReaction(container, name, url);
				return false;
			});
	}

	function addReaction(container, name, imageUrl) {
		callApi(container, 'AddReaction', {
				MessageId: container.children("[id*=hdnMessageId]").val(), Name: name, ImageUrl: imageUrl
			});
	}

	function removeReaction(container, name) {
		callApi(container, 'RemoveReaction', {
			MessageId: container.children("[id*=hdnMessageId]").val(), Name: name
		});
	}

	function callApi(container, action, requestBody) {
		$.post('MessageThreadDetail.aspx?CallBack=' + action, requestBody,
			function (reactionData) {
				if (reactionData) {
					container.data('reactions', reactionData);
					renderReactions(container, reactionData);
				}
				else {
					console.error(action + " failed");
				}
			}
		);
	}
})();
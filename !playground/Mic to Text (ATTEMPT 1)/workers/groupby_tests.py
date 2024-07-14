import datetime
from datetime import timedelta


# ----------------------------------------------------------------------------------------------------------------------------------------------
# These are focused on start time and width being within some fixed tolerance time
# ----------------------------------------------------------------------------------------------------------------------------------------------

def is_within_range(date1, date2, tolerance=timedelta(seconds=1)):
    # Check if two dates are within an acceptable range of each other
    return abs((date1 - date2).total_seconds()) <= tolerance.total_seconds()


def group_by_approximate_range(items):
    """
    Groups tuples with nearly the same daterange using approximate matching logic.

    Parameters:
    - items (list of tuple): A list where each element is a tuple in the format ((datetime, datetime)).

    Returns:
    dict: A dictionary keyed by the first datetime in each tuple, with values being lists of tuples that fall within the range.

    Example usage:
    >>> items = [
            (datetime(2023, 1, 1), datetime(2023, 1, 5)),
            (datetime(2023, 1, 3), datetime(2023, 1, 8))
        ]
    >>> grouped_items = group_by_approximate_range(items)

    {datetime(2023, 1, 1): [(datetime(2023, 1, 1), datetime(2023, 1, 5))],
     datetime(2023, 1, 3): [(datetime(2023, 1, 3), datetime(2023, 1, 8))]
    }
    """

    # Sort the list to handle grouping sequentially
    sorted_items = sorted((item for item in items), key=lambda x: x[0])

    grouped_items = {}

    for start_date, end_date in sorted_items:
        next_start_date = None

        # Iterate through the remaining list to find similar ranges
        for _, next_end_date in sorted_items[sorted_items.index((start_date, end_date))+1:]:
            if is_within_range(start_date, next_start_date):
                grouped_items.setdefault(start_date, []).append((start_date, end_date))
                break  # Stop searching for similar ranges once a match is found
            else:
                next_start_date = next_end_date

        # Add the last group if no similar range was found within tolerance limit
        if not grouped_items.get(start_date, False):
            grouped_items[start_date] = [(start_date, end_date)]

    return grouped_items






# ----------------------------------------------------------------------------------------------------------------------------------------------
# This revised function now calculates midpoints and widths dynamically. When checking if an item's range falls within another, it
# considers the range to be acceptable if it is within +-20% of the other item's width. This allows for more flexible grouping based on
# relative ranges rather than fixed tolerances or static date intervals.
# ----------------------------------------------------------------------------------------------------------------------------------------------

# Calculates the midpoint between two dates
def calculate_midpoint(start_date, end_date):
    return start_date + (end_date - start_date) / 2

def group_by_dynamic_ranges(items):
    # ... (other parts of the function remain unchanged)
    # Sort the list to handle grouping sequentially
    sorted_items = sorted((item for item in items), key=lambda x: x[0])
    grouped_items = {}

    for start_date, end_date in sorted_items:
        next_start_date = None

        for _, next_end_date in sorted_items[sorted_items.index((start_date, end_date))+1:]:
            midpoint = calculate_midpoint(start_date, next_start_date)
            width = (next_start_date - start_date).days / 2

            # Check if the range is within +-20% of another item's width
            for other_start, _ in sorted_items:
                if midpoint > other_start and midpoint < next_start_date:
                    similar_width = abs((other_start - start_date).days / 2) * 1.20
                    if (next_end_date - start_date).days <= similar_width or (next_start_date - other_start).days <= similar_width:
                        grouped_items.setdefault(start_date, []).append((start_date, end_date))
                        break  # Stop searching for similar ranges once a match is found

            # ... (rest of the loop remains unchanged)

    return grouped_items

















#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <math.h>

/*
NOTICE: the GC has memory leaks

i plan to throw this away and learn rust and make something good later
for now, this is just a hackjob to claim support of C

this is embedded within the compiler and then extracted and the // <INJECT__CODE> code
comments is where the terumi code is extracted to.
*/

// every time a list isn't big enough, it grows
// by this many times
#define LIST_GROW_MULTIPLIER 3

// defines how many fields there should be for an object
#define GC_OBJECT_FIELDS 3

#define TRUE 1
#define FALSE 0

#define TYPE_STRING 1
#define TYPE_NUMBER 2
#define TYPE_BOOLEAN 3	
#define TYPE_OBJECT 4

// #define DEBUG
#ifdef DEBUG
	#define DEBUG_PRINT(x) printf("[DBG] '%s'\n", x)
	#define NULL_CHECK(x, y) if (!x) printf("[WARN] %s is null\n", y);
#else
	#define DEBUG_PRINT(x) ;
	#define NULL_CHECK(x, y) ;
#endif

#define GET_TYPE(x) \
	(x == TYPE_STRING ? "string" \
	: x == TYPE_NUMBER ? "number" \
	: x == TYPE_BOOLEAN ? "boolean" \
	: x == TYPE_OBJECT ? "object" \
	: "unknown")

int* bool_true;
int* bool_false;

/* List for maintaining all variables */
struct List {

	// array of void* (pointers to data)
	void** data;

	// the length of 'data'
	int capacity;

	// the amount of elements actually in 'data'
	int elements;
};

// helper method to make a new list with a given capacity
struct List* new_list(int initialCapacity) {
	DEBUG_PRINT("new_list");
	struct List* list = malloc(sizeof(struct List));

	list->elements = 0;
	list->capacity = initialCapacity;
	list->data = (void**)malloc(sizeof(void*) * initialCapacity);

	return list;
}

// adds an element to a list
int add_item(struct List* list, void* item) {
	DEBUG_PRINT("add_item");
	NULL_CHECK(list, "list");
	NULL_CHECK(item, "item");

	// if the list has reached maximum capacity
	if (list->elements >= list->capacity) {

		// double the capacity
		int new_capacity = list->capacity * LIST_GROW_MULTIPLIER;

		// create a new array of void*
		void** new_memory = (void**)malloc(sizeof(void*) * new_capacity);

		// copy the old data to the new memory
		if (!memcpy(new_memory, list->data, sizeof(void*) * list->elements)) {
			printf("Unable to memcpy");
			return -1;
		}

		// we're no longer using this data
		free(list->data);

		list->data = new_memory;
		list->capacity = new_capacity;
	}

	// insert the item into the array
	int index = list->elements++;
	list->data[index] = item;

	return index;
}

/* Represents the value for a variable */
struct Value {
	int type;
	void* data;
};

struct Value* malloc_value() {
	DEBUG_PRINT("malloc_value");
	struct Value* value = malloc(sizeof(struct Value));
	value->type = 0;
	return value;
}

struct Value* malloc_values(int amount) {
	DEBUG_PRINT("malloc_values");
	struct Value* values = malloc(sizeof(struct Value) * amount);

	for (int i = 0; i < amount; i++) {
		values[i].type = 0;
	}

	return values;
}

struct Value* new_value_boolean(int boolean) {
	DEBUG_PRINT("new_value_boolean");
	struct Value* value = malloc_value();
	value->type = TYPE_BOOLEAN;

	if (boolean) {
		value->data = bool_true;
	} else {
		value->data = bool_false;
	}

	return value;
}

struct Value* new_value_int(int integer) {
	DEBUG_PRINT("new_value_int");
	int* intptr = malloc(sizeof(int));
	*intptr = integer;

	struct Value* value = malloc_value();
	value->type = TYPE_NUMBER;
	value->data = intptr;
	return value;
}

struct Value* new_value_string(char* str, int length) {
	DEBUG_PRINT("new_value_string");
	NULL_CHECK(str, "str");
	struct Value* value = malloc_value();
	value->type = TYPE_STRING;

	char* str_data = malloc(sizeof(char) * length);
	memcpy(str_data, str, sizeof(char) * length);
	value->data = str_data;

	return value;
}

/* GC */
struct GCObject {
	struct Value* fields;
};

struct GCObjectEntry {
	struct GCObject* data;
	int alive;
};

/*
the way the GC works in simple and alright for small projects.
we have a list of objects, and when we need to run the GC we figure out
which objects are still referenced
*/

struct List gc;

struct GCObject* to_gc_object(struct Value* value) {
	DEBUG_PRINT("to_gc_object");
	NULL_CHECK(value, "value");
	int position = *(int*)(value->data);
	struct GCObjectEntry* entry = gc.data[position];
	return entry->data;
}

/*
for when to run the GC,
we run it whenever the list capacity is higher than the last time it was high (gc_threshold).
^ in the new_value_object and free_value (case TYPE_OBJECT) functions

if the GC isn't under the threshold, we run it
if we don't collect more than 1% of the threshold, we increase the threshold by 2 fold.
if we collect more than 60% of the threshold, we decrease the threshold by 2 fold.
*/

// default threshold value
int gc_threshold = 1000000;

int should_run_gc() {
	DEBUG_PRINT("should_run_gc");
	if (gc.elements > gc_threshold) {
		return TRUE;
	}

	return FALSE;
}

void calc_threshold(int cleared) {
	DEBUG_PRINT("calc_threshold");
	int increase_threshold_percent = gc_threshold / 100;
	int downsize_threshold_percent = (float)gc_threshold / (float)(60.0 / 100.0);

	if (cleared < increase_threshold_percent) {
		// we didn't clear more than 1% of the threshold
		gc_threshold *= 2;
	}

	if (cleared >= downsize_threshold_percent) {
		// decrease the threshold
		gc_threshold /= 2;
	}
}

int recursive_run_gc(int* references) {
	DEBUG_PRINT("recursive_run_gc");
	NULL_CHECK(references, "references");
	// simple mark & sweep GC
	// this doesn't free cyclic references so it's best to avoid them
	memset(references, 0, sizeof(int) * gc.elements);

	// for each object,
	for (int i = 0; i < gc.elements; i++) {
		struct GCObjectEntry* gcobject_entry = gc.data[i];

		if (!(gcobject_entry->alive)) {
			continue;
		}

		struct GCObject* gcobj = gcobject_entry->data;

		// we find each object it references, and mark it as referenced
		// when something has no references it is completely unreferenced by anything
		for (int j = 0; j < GC_OBJECT_FIELDS; j++) {
			struct Value* value = gcobj->fields + j;

			if (value->type < 0
				|| value->type > TYPE_OBJECT) {
#ifdef DEBUG
				printf("[DBG] data.type is fudging wacky dude, for gc obj %i in field %i\n", i, j);
#endif
			}

			if (value->type == TYPE_OBJECT) {
				// and add it to the references list
				references[*(int*)(value->data)]++;
			}
		}
	}

	// sweep time
	int cleared = 0;

	// we want to free every object not referenced
	for (int i = 0; i < gc.elements; i++) {
		struct GCObjectEntry* gcobject_entry = gc.data[i];
		if (references[i] == 0 && gcobject_entry->alive) {
			// mark it as dead and free the data
			gcobject_entry->alive = FALSE;
			free(gcobject_entry->data);

			cleared++;
		}
	}

	// we want to recursively clean up the GC, to get all the objects
	if (cleared > 0) {
		return cleared + recursive_run_gc(references);
	}

	return cleared;
}

int run_gc() {
	DEBUG_PRINT("run_gc");
	int* references = malloc(sizeof(int) * gc.elements);
	int cleared = recursive_run_gc(references);
	free(references);

	// if we didn't clear anything, we don't want to compact the list
	if (cleared == 0) {
		return 0;
	}

	// now we're going to compact the GC list
	// the algorithm to be employed is simple:
	// from the beginning, find a dead item
	// from the end, find an alive item
	// swap, and continue until they meet in the middle

	int begin = 0;
	int end = gc.elements - 1;

	while (begin < end) {

		// find a dead item
		while (begin < gc.capacity) {
			struct GCObjectEntry* entry = gc.data[begin];

			if (!(entry->alive)) {
				break;
			}

			begin++;
		}

		// early exit
		// if begin == end, swapping is useless
		// if begin > end, then we probably couldn't find anything else to compact
		if (begin >= end) {
			break;
		}

		// find an alive item from the end
		while (end >= 0) {
			struct GCObjectEntry* entry = gc.data[end];

			if (entry->alive) {
				break;
			}

			end--;
		}

		// early exit
		if (begin <= end) {
			break;
		}

		// swap
		struct GCObjectEntry* begin_entry = gc.data[begin]; // dead one
		struct GCObjectEntry* end_entry = gc.data[end]; // alive one

		begin_entry->alive = TRUE;
		end_entry->alive = FALSE;

		// we don't need to worry aobut freeing begin_entry
		// since that's already done in the recursive_run_gc part
		begin_entry->data = end_entry->data;
	}

	// now we've completely compacted the list
	// let's find out how many entries are alive
	int i = 0;
	while (((struct GCObjectEntry*)(gc.data[i]))->alive) {
		i++;
	}

	// i is on a dead item
	// we want the last alive item
	int new_length = i - 1

	// but we actually want the length
		+ 1;

	gc.elements = new_length;

	int leftover_capacity = gc.capacity - new_length;

	// it's good to leave some free space for future objects to use
	// but if we have too much capacity, it could give us a bad time
	// thus, if we're only using 10 objects but we have 1000 objects of free space,
	// we want to reclaim enough so that it's 10 objects -> 20 free

	// 10 * 100 >= 1000
	if (gc.elements * 100 >= gc.capacity) {
		// 10 * 2 = 20
		struct List* new_gc = new_list(gc.elements * 2);
		memcpy(new_gc->data, gc.data, gc.elements * sizeof(struct GCObjectEntry*));

		for (int i = gc.elements; i < gc.capacity; i++) {
			free(gc.data[i]);
		}

		free(gc.data);
		gc.data = new_gc->data;
	}

	return cleared;
}

inline void maybe_run_gc() {
	DEBUG_PRINT("maybe_run_gc");
	if (should_run_gc()) {
		calc_threshold(run_gc());
	}
}

struct Value* new_object() {
	DEBUG_PRINT("new_object");

	struct GCObject* gcobject = malloc(sizeof(struct GCObject));
	gcobject->fields = malloc_values(GC_OBJECT_FIELDS);
	
	struct GCObjectEntry* entry = malloc(sizeof(struct GCObjectEntry));
	entry->alive = TRUE;
	entry->data = gcobject;

	int gc_index = add_item(&gc, entry);
	struct Value* value = new_value_int(gc_index);
	value->type = TYPE_OBJECT;

	maybe_run_gc();
	return value;
}

void delete_value(struct Value* data) {
	DEBUG_PRINT("delete_value");
	NULL_CHECK(data, "data");
	int type = data->type;

	switch (type) {

		case TYPE_BOOLEAN: {
			// don't free the value (we use bool_true and bool_false)
			free(data);
		}
		break;

		case TYPE_STRING:
		case TYPE_NUMBER: {
			free(data->data);
			free(data);
		}
		break;

		case TYPE_OBJECT: {
			// we're not using the value anymore so we can free it
			// we just can't free the gcobject if it still has references
			free(to_gc_object(data));
			free(data);
			maybe_run_gc();
		}
		break;

		// case '0' is allocated using malloc_value which is fine
		case 0: {
			free(data);
		}

		default: {
			printf("WARNING: unknown value type %i\n", type);
			free(data);
		}
	}

}

int free_value(struct Value* data) {
	DEBUG_PRINT("free_value");
	NULL_CHECK(data, "data");
	int type = data->type;

	switch (type) {

		case TYPE_BOOLEAN: {
			// don't free the value (we use bool_true and bool_false)
		}
		break;

		case TYPE_STRING:
		case TYPE_NUMBER: {
			// free the data at the value
			free(data->data);
		}
		break;

		case TYPE_OBJECT: {
			// we're not using the value anymore so we can free it
			// we just can't free the gcobject if it still has references
			free(data);
			maybe_run_gc();
		}
		break;

		// case '0' is allocated using malloc_value which is fine
		case 0: {
			return TRUE;
		}

		default: {
			printf("WARNING: unknown value type %i\n", type);
			return FALSE;
		}
	}

	// we leave the caller to free the actual value
	// free(data);
	return TRUE;
}

int assign_gc_object(struct Value* dst, struct Value* src) {
	DEBUG_PRINT("assign_gc_object");
	NULL_CHECK(dst, "dst");
	NULL_CHECK(src, "src");
	if (src->type != TYPE_OBJECT) {
		printf("cannot assign gc object - source isn't a TYPE_OBJECT\n");
		return FALSE;
	}

	int* gcobject = src->data;

	dst->type = TYPE_OBJECT;
	dst->data = gcobject;
	return TRUE;
}

/* terumi instructions */

inline struct Value* load_string(const char* value) {
	return new_value_string(value, strlen(value));
}

inline struct Value* load_number(int number) {
	return new_value_int(number);
}

inline struct Value* load_boolean(int boolean) {
	return new_value_boolean(boolean);
}

inline struct Value* load_parameter(struct Value* parameters, int index) {
	return &(parameters[index]);
}

inline void assign(struct Value* src, struct Value* target) {
	// TODO: figure out when to free the data

	// just malloc'd
	if (target->type == 0) {
		target->type = src->type;
		target->data = src->data;
		return;
	}

	if (target->type == src->type) {
		if (target->type == TYPE_OBJECT) {
			assign_gc_object(target, src);
			return;
		}

		target->data = src->data;
		return;
	}

	if (target->type != src->type) {
		// have to do type conversions
		if (target->type == TYPE_STRING) {
			if (src->type == TYPE_NUMBER) {
				char* buffer = malloc(sizeof(char) * 12);
				target->data = itoa(*(int*)(src->data), buffer, 10);
				return;
			}

			if (src->type == TYPE_BOOLEAN) {
				char* buffer = malloc(sizeof(char) * 5);
				target->data = buffer;
				return;
			}

			if (src->type == TYPE_OBJECT) {
				printf("[PANIC] cannot assign a '%s' to a '%s'\n", GET_TYPE(src->type), GET_TYPE(target->type));
				return;
			}
		}

		if (target->type == TYPE_NUMBER) {
			if (src->type == TYPE_STRING) {
				int* data = malloc(sizeof(int));
				*data = atoi(src->data);
				target->data = data;
				return;
			}

			if (src->type == TYPE_BOOLEAN) {
				// booleans are just ints in C
				target->data = src->data;
				return;
			}

			if (src->type == TYPE_OBJECT) {
				printf("[PANIC] cannot assign a '%s' to a '%s'\n", GET_TYPE(src->type), GET_TYPE(target->type));
				return;
			}
		}

		if (target->type == TYPE_BOOLEAN) {
			if (src->type == TYPE_STRING
				|| src->type == TYPE_OBJECT) {
				printf("[PANIC] cannot assign a '%s' to a '%s'\n", GET_TYPE(src->type), GET_TYPE(target->type));
				return;
			}

			// only number is possibly left
			// booleans are ints
			target->data = src->data;
			return;
		}

		if (target->type == TYPE_OBJECT) {
			printf("[PANIC] cannot assign a '%s' to a '%s'\n", GET_TYPE(src->type), GET_TYPE(target->type));
			return;
		}

		printf("[PANIC] unknown object conversion from '%s' to '%s'\n", GET_TYPE(src->type), GET_TYPE(target->type));
		return;
	}
}

inline void set_helper(struct GCObject* gcobj, int fieldId, struct Value* data) {
	gcobj->fields[fieldId] = *data;
}

inline void set_field(struct Value* obj, int fieldId, struct Value* data) {
	if (obj->type != TYPE_OBJECT) {
		printf("[PANIC] cannot set object field on non object '%s'\n", GET_TYPE(obj->type));
		return;
	}

	struct GCObject* gcobj = to_gc_object(obj);
	set_helper(gcobj, fieldId, data);
}

inline struct Value* get_field(struct Value* obj, int fieldId) {
	if (obj->type != TYPE_OBJECT) {
		printf("[PANIC] cannot get field of non object '%s'\n", GET_TYPE(obj->type));
		return 0;
	}

	struct GCObject* gcobj = to_gc_object(obj);
	return gcobj->fields + fieldId;
}

inline struct Value* new(){
	return new_object();
}

inline int do_comparison(struct Value* boolean) {
	if (boolean->type != TYPE_BOOLEAN) {
		printf("[PANIC] cannot do comparison with non boolean '%s'\n", GET_TYPE(boolean->type));

		// if the object is a number, we'll let it slide (secretly)
		if (boolean->type == TYPE_NUMBER) {
			return *(int*)(boolean->data);
		}

		return FALSE;
	}

	return *(int*)(boolean->data);
}

// compiler commands

inline struct Value* cc_target_name() {
	return load_string("c");
}

inline struct Value* cc_panic(struct Value* message) {
	if (message->type != TYPE_STRING) {
		printf("[PANIC] cannot 'panic' on non string '%s'\n", GET_TYPE(message->type));
		return load_boolean(FALSE);
	}

	printf("[PANIC] %s\n", (char*)(message->data));
	return load_boolean(FALSE);
}

inline void cc_command(struct Value* command) {
	if (command->type != TYPE_STRING) {
		printf("[PANIC] cannot 'command' on non string '%s'\n", GET_TYPE(command->type));
		return;
	}

	system((char*)(command->data));
}

inline struct Value* cc_is_supported(struct Value* item) {
	if (item->type != TYPE_STRING) {
		printf("[PANIC] cannot 'is_supported' on non string '%s'\n", GET_TYPE(item->type));
		return load_boolean(FALSE);
	}

	char* value = (char*)(item->data);
	// TODO: check what's supported
	return load_boolean(TRUE);
}

inline void cc_println(struct Value* message) {
	if (message->type != TYPE_STRING) {
		printf("[PANIC] cannot 'println' on non string '%s'\n", GET_TYPE(message->type));
		return;
	}

	printf("%s\n", (char*)(message->data));
}

inline struct Value* cc_operator_and(struct Value* a, struct Value* b) {
	if (a->type != TYPE_BOOLEAN
		|| b->type != TYPE_BOOLEAN) {
		printf("[PANIC] cannot 'operator_and' on non booleans '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	int bool_a = a->data == bool_true;
	int bool_b = b->data == bool_true;

	return load_boolean(bool_a && bool_b);
}

inline struct Value* cc_operator_or(struct Value* a, struct Value* b) {
	if (a->type != TYPE_BOOLEAN
		|| b->type != TYPE_BOOLEAN) {
		printf("[PANIC] cannot 'operator_or' on non booleans '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	int bool_a = a->data == bool_true;
	int bool_b = b->data == bool_true;

	return load_boolean(bool_a || bool_b);
}

inline struct Value* cc_operator_not(struct Value* a) {
	if (a->type != TYPE_BOOLEAN) {
		printf("[PANIC] cannot 'operator_not' on non boolean '%s'\n", GET_TYPE(a->type));
		return load_boolean(FALSE);
	}

	int bool_a = a->data == bool_true;

	return load_boolean(!bool_a);
}

inline struct Value* cc_operator_equal_to(struct Value* a, struct Value* b) {
	if (a->type != b->type) {
		printf("[PANIC] cannot 'operator_equal_to' on variables of dissimilar types '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	if (a->type == TYPE_STRING) {
		return load_boolean(strcmp(a->data, b->data) == 0);
	}

	if (a->type == TYPE_NUMBER
		|| a->type == TYPE_BOOLEAN) {
		return load_boolean(*(int*)(a->data) == *(int*)(b->data));
	}

	if (a->type == TYPE_OBJECT) {
		struct GCObject* left = to_gc_object(a);
		struct GCObject* right = to_gc_object(b);

		// reference equality only
		return load_boolean(left == right);
	}

	printf("[PANIC] cannot 'operator_equal_to' on variable types '%s'\n", GET_TYPE(a->type));
	return load_boolean(FALSE);
}

inline struct Value* cc_operator_not_equal_to(struct Value* a, struct Value* b) {
	return cc_operator_not(cc_operator_equal_to(a, b));
}

inline struct Value* cc_operator_less_than(struct Value* a, struct Value* b) {
	if (a->type != b->type) {
		printf("[PANIC] cannot 'operator_less_than' on variables of dissimilar types '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	if (a->type != TYPE_NUMBER) {
		printf("[PANIC] cannot compare types of not type '%s'\n", GET_TYPE(a->type));
		return load_boolean(FALSE);
	}

	return load_boolean(*(int*)a->data < *(int*)b->data);
}

inline struct Value* cc_operator_greater_than(struct Value* a, struct Value* b) {
	if (a->type != b->type) {
		printf("[PANIC] cannot 'operator_greater_than' on variables of dissimilar types '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	if (a->type != TYPE_NUMBER) {
		printf("[PANIC] cannot compare types of not type '%s'\n", GET_TYPE(a->type));
		return load_boolean(FALSE);
	}

	return load_boolean(*(int*)a->data > * (int*)b->data);
}

inline struct Value* cc_operator_less_than_or_equal_to(struct Value* a, struct Value* b) {
	if (a->type != b->type) {
		printf("[PANIC] cannot 'operator_less_than_or_equal_to' on variables of dissimilar types '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	if (a->type != TYPE_NUMBER) {
		printf("[PANIC] cannot compare types of not type '%s'\n", GET_TYPE(a->type));
		return load_boolean(FALSE);
	}

	return load_boolean(*(int*)a->data <= *(int*)b->data);
}

inline struct Value* cc_operator_greater_than_or_equal_to(struct Value* a, struct Value* b) {
	if (a->type != b->type) {
		printf("[PANIC] cannot 'operator_greater_than_or_equal_to' on variables of dissimilar types '%s' and '%s'\n", GET_TYPE(a->type), GET_TYPE(b->type));
		return load_boolean(FALSE);
	}

	if (a->type != TYPE_NUMBER) {
		printf("[PANIC] cannot compare types of not type '%s'\n", GET_TYPE(a->type));
		return load_boolean(FALSE);
	}

	return load_boolean(*(int*)a->data >= *(int*)b->data);
}

inline struct Value* cc_operator_add(struct Value* a, struct Value* b) {
	if (a->type == TYPE_NUMBER && b->type == TYPE_NUMBER) {
		return load_number(*(int*)a->data + *(int*)b->data);
	}

	if (a->type == TYPE_STRING && b->type == TYPE_STRING) {
		char* concat = malloc(sizeof(char) * (strlen((char*)a->data) + strlen((char*)b->data)));
		strcat(concat, (char*)a->data);
		strcat(concat, (char*)b->data);
		return load_string(concat);
	}

	printf("[PANIC] cannot 'operator_add' types of not both %s or %s. received '%s' and '%s'", GET_TYPE(TYPE_NUMBER), GET_TYPE(TYPE_STRING), GET_TYPE(a->type), GET_TYPE(b->type));
	return load_boolean(FALSE);
}

inline struct Value* cc_operator_negate(struct Value* a) {
	if (a->type != TYPE_NUMBER) {
		printf("[PANIC] cannot 'operator_negate' non number '%s'", GET_TYPE(a->type));
		return a;
	}

	return load_number(*(int*)a->data * -1);
}

inline struct Value* cc_operator_subtract(struct Value* a, struct Value* b) {
	if (a->type != TYPE_NUMBER || b->type != TYPE_NUMBER) {
		printf("[PANIC] cannot 'operator_subtract' non numbers '%s' and '%s'", GET_TYPE(a->type), GET_TYPE(b->type));
		return a;
	}

	return load_number(*(int*)a->data - *(int*)b->data);
}

inline struct Value* cc_operator_multiply(struct Value* a, struct Value* b) {
	if (a->type != TYPE_NUMBER || b->type != TYPE_NUMBER) {
		printf("[PANIC] cannot 'operator_multiply' non numbers '%s' and '%s'", GET_TYPE(a->type), GET_TYPE(b->type));
		return a;
	}

	return load_number(*(int*)a->data * *(int*)b->data);
}

inline struct Value* cc_operator_divide(struct Value* a, struct Value* b) {
	if (a->type != TYPE_NUMBER || b->type != TYPE_NUMBER) {
		printf("[PANIC] cannot 'operator_divide' non numbers '%s' and '%s'", GET_TYPE(a->type), GET_TYPE(b->type));
		return a;
	}

	return load_number(*(int*)a->data / *(int*)b->data);
}

inline struct Value* cc_operator_exponent(struct Value* a, struct Value* b) {
	if (a->type != TYPE_NUMBER || b->type != TYPE_NUMBER) {
		printf("[PANIC] cannot 'operator_exponent' non numbers '%s' and '%s'", GET_TYPE(a->type), GET_TYPE(b->type));
		return a;
	}

	return load_number((int)pow(*(int*)a->data, *(int*)b->data));
}

// <INJECT__CODE>

int main(int argc, char** argv) {
	DEBUG_PRINT("main");
	/* INIT */
	gc = *new_list(gc_threshold);

	// when we give Value a boolean, we want to reuse the same booleans
	bool_true = malloc(sizeof(int));
	bool_false = malloc(sizeof(int));
	
	*bool_false = FALSE;
	*bool_true = TRUE;

	// run code
// <INJECT__RUN>

	// end code
	free(bool_true);
	free(bool_false);
	return 0;
}
extends TextureProgress

onready var healthyTexture = preload("res://Assets/Sprites/Ships/health-green.png")
onready var damagedTexture = preload("res://Assets/Sprites/Ships/health-orange.png")
onready var brokenTexture = preload("res://Assets/Sprites/Ships/health-red.png")

const IS_HEALTHY: int = 70
const IS_DAMAGED: int = 30

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if value >= IS_HEALTHY:
		texture_progress = healthyTexture
	elif value > IS_DAMAGED:
		texture_progress = damagedTexture
	else:
		texture_progress = brokenTexture
